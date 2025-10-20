using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using Microsoft.Extensions.Logging;

namespace LinKb.Core;

[DebuggerStepThrough, StackTraceHidden]
public static class Log
{
    private readonly record struct LogInfo(LogLevel Level, string Message);

    private static readonly Queue<LogInfo> LogQueue = new();
    private static readonly Lock LogLock = new();

    private static readonly AutoResetEvent? ResetEvent;

    [DebuggerHidden, StackTraceHidden]
    static Log()
    {
        var cancellationSource = new CancellationTokenSource();
        ResetEvent = new AutoResetEvent(false);
        var args = new ThreadArgs
        {
            AutoResetEvent = ResetEvent,
            CancellationToken = cancellationSource.Token
        };
        var thread = new Thread(LogThread)
        {
            IsBackground = true
        };

        thread.Start(args);

        // detect process exit to clean up the thread
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            cancellationSource.Cancel();
            ResetEvent.WaitOne(); // wait for the thread to finish

            // dispose resources
            ResetEvent.Dispose();
            cancellationSource.Dispose();
        };
    }

    private class ThreadArgs
    {
        public required AutoResetEvent AutoResetEvent { get; init; }
        public required CancellationToken CancellationToken { get; init; }
    }

    [DebuggerHidden, StackTraceHidden, DebuggerStepThrough]
    private static async void LogThread(object? argsObj)
    {
        try
        {
            if (argsObj is not ThreadArgs args)
            {
                await Console.Error.WriteLineAsync("Thread arguments must be ThreadArgs object");
                return;
            }

            bool canChangeColor, canLogToConsole = true;

            try
            {
                Console.ResetColor();
                canChangeColor = true;
            }
            catch (SecurityException)
            {
                canChangeColor = false;
            }
            catch (IOException)
            {
                canChangeColor = false;
                canLogToConsole = false;
            }

            var token = args.CancellationToken;
            var resetEvent = args.AutoResetEvent;
            var waitHandles = new[] { resetEvent, token.WaitHandle };
            while (true)
            {
                if (WaitHandle.WaitAny(waitHandles) != 0)
                {
                    // cancellation token (index 1) was signaled
                    break;
                }

                int remainingCount;

                do
                {
                    LogInfo info;
                    lock (LogLock)
                    {
                        if (!LogQueue.TryDequeue(out info))
                        {
                            break;
                        }

                        remainingCount = LogQueue.Count;

                        if (remainingCount == 0)
                            resetEvent.Reset();
                    }

                    if (canLogToConsole)
                    {
                        await Output(canChangeColor, info);
                    }
                } while (remainingCount > 0);
            }

            resetEvent.Set();
        }
        catch (Exception e)
        {
            try
            {
                await Console.Error.WriteLineAsync($"Logging thread terminated unexpectedly: {e}");
            }
            catch
            {
                // ignored
            }
        }

        return;

        static async Task Output(bool canChangeColor, LogInfo info)
        {
            if (!canChangeColor)
            {
                await Console.Out.WriteLineAsync(info.Message);
                return;
            }

            switch (info.Level)
            {
                case LogLevel.Information:
                case LogLevel.Debug:
                    await Console.Out.WriteLineAsync(info.Message);
                    break;
                case LogLevel.Trace:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    await Console.Out.WriteLineAsync(info.Message);
                    Console.ResetColor();
                    break;
                case LogLevel.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    await Console.Out.WriteLineAsync(info.Message);
                    Console.ResetColor();
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    Console.ForegroundColor = ConsoleColor.Red;
                    await Console.Error.WriteLineAsync(info.Message);
                    Console.ResetColor();
                    break;
                default:
                    await Console.Out.WriteLineAsync(info.Message);
                    break;
            }
        }
    }

    [DebuggerHidden, StackTraceHidden, DebuggerStepThrough]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public static void Write(string? msg, LogLevel level, object? owner = null)
    {
        if (level == LogLevel.None || msg == null)
            return;

        if (!Enum.IsDefined(level))
        {
            throw new ArgumentOutOfRangeException(nameof(level), $"Invalid log level {level}");
        }

        msg = owner switch
        {
            string s => $"[{s}] {msg}",
            not null => $"[{owner.GetType().Name}] {msg}",
            _ => msg
        };

        if (level is LogLevel.Error or LogLevel.Critical or LogLevel.Trace)
        {
            // add stack trace to message
            msg = $"{msg}\n{new StackTrace(2, true)}";
        }

        lock (LogLock)
        {
            LogQueue.Enqueue(new LogInfo(level, msg));
        }

        ResetEvent?.Set();
    }

    [DebuggerHidden, StackTraceHidden, DebuggerStepThrough, Conditional("DEBUG")]
    public static void Debug(string? msg, object? owner = null)
    {
        Write(msg, LogLevel.Debug, owner);
    }

    [DebuggerHidden, StackTraceHidden, DebuggerStepThrough, Conditional("DEBUG")]
    public static void Debug(object msg, object? owner = null)
    {
        Write(msg.ToString(), LogLevel.Debug, owner);
    }


    [DebuggerHidden, StackTraceHidden, DebuggerStepThrough]
    public static void Info(string? msg, object? owner = null) => Write(msg, LogLevel.Information, owner);

    [DebuggerHidden, StackTraceHidden, DebuggerStepThrough]
    public static void Info(object msg, object? owner = null) => Write(msg.ToString(), LogLevel.Information, owner);

    [DebuggerHidden, StackTraceHidden, DebuggerStepThrough]
    public static void Error(string? msg, object? owner = null) => Write(msg, LogLevel.Error, owner);

    [DebuggerHidden, StackTraceHidden, DebuggerStepThrough]
    public static void Error(object msg, object? owner = null) => Write(msg.ToString(), LogLevel.Error, owner);

    [DebuggerHidden, StackTraceHidden, DebuggerStepThrough]
    public static void Warn(string? msg, object? owner = null) => Write(msg, LogLevel.Warning, owner);

    [DebuggerHidden, StackTraceHidden, DebuggerStepThrough]
    public static void Warn(object msg, object? owner = null) => Write(msg.ToString(), LogLevel.Warning, owner);
}