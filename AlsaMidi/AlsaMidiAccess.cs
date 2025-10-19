using AlsaSharp;
using Commons.Music.Midi;

namespace Midi.NET.Alsa;

public class AlsaMidiAccess : IMidiAccess2
{
    public Task<IMidiInput> OpenInputAsync(string portId)
    {
        var api = new AlsaMidiApi();
        // get matching with id
        foreach (var input in api.EnumerateAvailableInputPorts())
        {
            if (input.Id == portId)
            {
                var portInfo = api.CreateInputConnectedPort(input, portId);
                var device = new AlsaMidiInputDevice(portInfo, api);
                return Task.FromResult<IMidiInput>(device);
            }
        }
        
        return Task.FromResult<IMidiInput>(null!);
    }

    public Task<IMidiOutput> OpenOutputAsync(string portId)
    {
        var api = new AlsaMidiApi();
        // get matching with id
        foreach (var output in api.EnumerateAvailableOutputPorts())
        {
            if (output.Id == portId)
            {
                var portInfo = api.CreateOutputConnectedPort(output, portId);
                var device = new AlsaMidiOutputDevice(portInfo, api);
                return Task.FromResult<IMidiOutput>(device);
            }
        }
        
        return Task.FromResult<IMidiOutput>(null!);
    }

    public IEnumerable<IMidiPortDetails> Inputs
    {
        get
        {
            var api = new AlsaMidiApi();
            foreach (var input in api.EnumerateAvailableInputPorts())
            {
                yield return new AlsaMidiPortDetails(input);
            }
        }
    }

    public IEnumerable<IMidiPortDetails> Outputs
    {
        get
        {
            var api = new AlsaMidiApi();
            foreach (var output in api.EnumerateAvailableOutputPorts())
            {
                yield return new AlsaMidiPortDetails(output);
            }
        }
    }

    // todo - realtime connection monitoring
    public event EventHandler<MidiConnectionEventArgs>? StateChanged;
    public MidiAccessExtensionManager ExtensionManager { get; } = new AlsaMidiAccessExtensionManager();
}

internal class AlsaMidiInputDevice : IMidiInput
{
    private readonly AlsaPortInfo _portInfo;
    private readonly AlsaMidiApi _api;
    private bool _isDisposed;
    
    public event EventHandler<MidiReceivedEventArgs>? MessageReceived;
    public IMidiPortDetails Details { get; }
    public MidiPortConnectionState Connection { get; }
    
    private readonly byte[] _buffer = new byte[1024];

    public AlsaMidiInputDevice(AlsaPortInfo portInfo, AlsaMidiApi api)
    {
        _portInfo = portInfo;
        Details = new AlsaMidiPortDetails(portInfo);
        Connection = MidiPortConnectionState.Open;
        _api = api;
        api.Input.StartListening(portInfo.Port, _buffer, OnReceived);
    }

    private void OnReceived(byte[] bytes, int offset, int length)
    {
        var midiEventArgs = new MidiReceivedEventArgs()
        {
            Data = bytes, Length = length, Start = offset, Timestamp = 0
        };
        MessageReceived?.Invoke(this, midiEventArgs);
    }

    public Task CloseAsync()
    {
        if (_isDisposed)
            return Task.CompletedTask;
        
        _isDisposed = true;
        _portInfo.Dispose();
        _api.Input.StopListening();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;
        
        _isDisposed = true;
        CloseAsync().Wait();
    }
}

internal class AlsaMidiOutputDevice : IMidiOutput
{
    private readonly AlsaPortInfo _portInfo;
    private readonly AlsaMidiApi _api;
    private bool _isDisposed;

    public AlsaMidiOutputDevice(AlsaPortInfo portInfo, AlsaMidiApi api)
    {
        _portInfo = portInfo;
        Details = new AlsaMidiPortDetails(portInfo);
        Connection = MidiPortConnectionState.Open;
        _api = api;
    }

    public Task CloseAsync()
    {
        if (_isDisposed)
            return Task.CompletedTask;
        
        _isDisposed = true;
        _portInfo.Dispose();
        //_api.Output.StopListening();
        return Task.CompletedTask;
    }

    public IMidiPortDetails Details { get; }

    public MidiPortConnectionState Connection { get; }

    public void Dispose()
    {
        if (_isDisposed)
            return;
        
        _isDisposed = true;
        CloseAsync().Wait();
    }

    public void Send(byte[] mevent, int offset, int length, long timestamp)
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(AlsaMidiOutputDevice));
        }
        _api.Output.Send(_portInfo.Port, mevent, offset, length);
        _api.Output.ResetPoolOutput();
    }
}

internal record AlsaMidiPortDetails : IMidiPortDetails
{
    public string Id { get; }
    public string Manufacturer { get; }
    public string Name { get; }
    public string Version { get; }

    public AlsaMidiPortDetails(AlsaPortInfo info)
    {
        Id = info.Id;
        Manufacturer = info.Manufacturer;
        Name = info.Name;
        Version = info.Version;
    }

    // explicit cast
    public static explicit operator AlsaMidiPortDetails(AlsaPortInfo info) => new(info);
}

internal class AlsaMidiAccessExtensionManager : MidiAccessExtensionManager
{
}