namespace InputHooks;

public readonly ref struct KeyboardEventArgs
{
    public ReadOnlySpan<KeyCode> KeyCodes { get; init; }
    public bool IsDown { get; init; }
    public float Pressure01 { get; init; }
    public long Timestamp { get; init; }
    public int DeviceId { get; init; }
    public bool IsSimulated { get; init; }
}