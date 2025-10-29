#undef STATS_DEBUG
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using LinKb.Application;
using Midi.Net;
using Midi.Net.MidiUtilityStructs;

namespace LinKb.Core;

public partial class MidiKeyboardGrid
{
    private readonly ConcurrentQueue<PadStatusEvent> _padStatusEvents = new();
    private readonly Stopwatch _eventQueueStopwatch = new();

    internal void ProcessEventQueue()
    {
#if STATS_DEBUG
        _eventQueueStopwatch.Restart();
        var processCount = 0;
        var acceptedCount = 0;
        var eventsTriggered = 0;
#endif

        while (_padStatusEvents.TryDequeue(out var evt))
        {
#if STATS_DEBUG
            ++processCount;
#endif
            if (evt.ColX >= Width)
            {
                Log.Debug($"Discarding event for out-of-bounds column: {evt.ColX}");
                continue;
            }

            if (evt.RowY >= Height)
            {
                Log.Debug($"Discarding event for out-of-bounds row: {evt.RowY}");
                continue;
            }

#if STATS_DEBUG
            ++acceptedCount;
#endif

            ref var status = ref _pads[evt.ColX, evt.RowY];
            var statusCopy = status;
            evt.ApplyTo(ref status);
            if (statusCopy.IsPressed != status.IsPressed)
            {
#if STATS_DEBUG
                ++eventsTriggered;
#endif
                OnPadPress(evt.ColX, evt.RowY, status.IsPressed);
            }

            // todo - events for other types of things like slides, etc
            if (statusCopy.Axes != status.Axes)
            {
            }

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (statusCopy.Velocity01 != status.Velocity01)
            {
            }
        }

#if STATS_DEBUG
        if (processCount > 0)
        {
            Log.Debug($"Processed {processCount} events, {acceptedCount} accepted, " +
                      $"{eventsTriggered} triggered in {_eventQueueStopwatch.ElapsedMilliseconds}ms");
        }
#endif

    }

    private void BeginReceiveThread()
    {
        if (_device is not IGridController controller)
        {
            throw new InvalidOperationException($"Device does not implement {nameof(IGridController)}.");
        }

        var parseMidiThread = new Thread(ParseMidi)
        {
            IsBackground = false,
            Name = "Parse MIDI Thread"
        };

        parseMidiThread.Start(new ThreadArgs
        {
            CancellationToken = _cancellationTokenSource.Token,
            EventWaitHandle = _eventWaitHandle,
            // ReSharper disable once InconsistentlySynchronizedField
            MidiEventQueue = _midiEventQueue,
            PadStatusEvents = _padStatusEvents,
            QueueLock = _queueLock,
            Controller = controller
        });

        _device.MidiDevice.MidiReceived += OnMidiReceived;
    }

    // todo - this should be an interface member that returns a desired result based on the midi
    // that way we're not hard-coded to linnstrument at this level
    private void OnMidiReceived(object? sender, ReadOnlyMemory<MidiEvent> e)
    {
        var values = e.Span;
        // Handle MIDI event
        // enqueue to different thread to avoid blocking the receiver
        lock (_queueLock)
        {
            for (int i = 0; i < values.Length; ++i)
            {
                _midiEventQueue.Enqueue(values[i]);
            }
            _eventWaitHandle.Set();
        }
    }

    private static void ParseMidi(object? obj)
    {
        var args = (ThreadArgs)obj!;
        var token = args.CancellationToken;
        var resetEvent = args.EventWaitHandle;
        var handles = new[] { token.WaitHandle, resetEvent.WaitHandle };
        var eventQueue = args.MidiEventQueue;
        var controller = args.Controller;
        var padStatusEvents = args.PadStatusEvents;
        List<MidiEvent> midiEvents = [];

#if DEBUG
        var stopwatch = new Stopwatch();
        ulong tWait = 0;
        ulong tParse = 0;
        int maxQueueCount = 0;
        double maxParseRatio = 0;
        double maxTicksPerBatch = 0;
        ulong totalParseCount = 0;
#endif

        try
        {
            while (!token.IsCancellationRequested)
            {
#if DEBUG
                stopwatch.Restart();
#endif
                if (!resetEvent.IsSet && WaitHandle.WaitAny(handles) == 0)
                {
                    break;
                }

#if DEBUG
                var waitElapsed = stopwatch.ElapsedTicks;
                tWait += (ulong)waitElapsed;
                stopwatch.Restart();
#endif

                // dequeue all events prior to parsing to minimize lock time
                lock (args.QueueLock)
                {
                    while (eventQueue.TryDequeue(out var midiEvent))
                    {
                        midiEvents.Add(midiEvent);
                    }

                    resetEvent.Reset();
                }

                if (midiEvents.Count == 0)
                {
                    continue;
                }

                var any = false;

                for (int i = 0; i < midiEvents.Count; ++i)
                {
                    if (controller.TryParseMidiEvent(midiEvents[i], out var padStatusEvent, out var error) && padStatusEvent != null)
                    {
                        padStatusEvents.Enqueue(padStatusEvent.Value);
                        any = true;
                    }
                    else
                    {
                        Log.Warn(error);
                    }
                }

                if (any)
                {
                    Daemon.InputTrigger.Set();
                }

#if DEBUG
                var midiEventsCount = midiEvents.Count;
                maxQueueCount = Math.Max(maxQueueCount, midiEventsCount);
                var parseElapsed = stopwatch.ElapsedTicks;
                var ticksPerMidiEvent = parseElapsed / (double)midiEventsCount;
                maxTicksPerBatch = Math.Max(maxTicksPerBatch, ticksPerMidiEvent);

                tParse += (ulong)parseElapsed;
                totalParseCount += (ulong)midiEventsCount;
                var ratio = parseElapsed / (double)(waitElapsed + parseElapsed);
                maxParseRatio = Math.Max(maxParseRatio, ratio);
#endif

                midiEvents.Clear();
            }
        }
        catch (Exception ex)
        {
            Log.Error("Exception in MIDI thread: " + ex);
        }

#if DEBUG
        var totalTime = tWait + tParse;
        if (totalTime != 0)
        {
            var waitRatio = tWait / (double)totalTime;
            var parseRatio = tParse / (double)totalTime;

            var sb = new StringBuilder(512);
            var toMs = 1000d / Stopwatch.Frequency;
            var maxMsPerBatch = maxTicksPerBatch * toMs;
            var averageMsPerMidiEvent = tParse * toMs / totalParseCount;
            sb.AppendLine("\nMIDI thread stats:")
                .Append("Wait time: ").Append(tWait * toMs)
                .Append("ms (").Append(waitRatio.ToString("P2")).AppendLine(")")
                .Append("Parse time: ").Append(tParse * toMs)
                .Append("ms (").Append(parseRatio.ToString("P2")).AppendLine(")")
                .Append("Average ms per MIDI event: ").Append(averageMsPerMidiEvent.ToString("F4"))
                .Append(" (Rate: ").Append((totalParseCount / (double)tParse).ToString("F2")).AppendLine("/ms)")
                .Append("Max time for a single midi event batch: ").Append(maxMsPerBatch.ToString("F4"))
                .AppendLine("ms")
                .Append("Max parse ratio: ").AppendLine(maxParseRatio.ToString("P2"))
                .Append("Max queue count: ").AppendLine(maxQueueCount.ToString(CultureInfo.InvariantCulture))
                .Append("Total MIDI events parsed: ").AppendLine(totalParseCount.ToString(CultureInfo.InvariantCulture))
                .Append("MIDI events parsed per second: ").AppendLine((totalParseCount / totalTime).ToString("F2"));
            Log.Debug(sb.ToString());
        }
#endif
    }

    private class ThreadArgs
    {
        public required CancellationToken CancellationToken { get; init; }
        public required ManualResetEventSlim EventWaitHandle { get; init; }
        public required Queue<MidiEvent> MidiEventQueue { get; init; }
        public required ConcurrentQueue<PadStatusEvent> PadStatusEvents { get; init; }
        public required Lock QueueLock { get; init; }
        public required IGridController Controller { get; init; }
    }

    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly ManualResetEventSlim _eventWaitHandle = new(false);
    private readonly Lock _queueLock = new();
    private readonly Queue<MidiEvent> _midiEventQueue = new();
}