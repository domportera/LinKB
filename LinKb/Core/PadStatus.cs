using System.Numerics;

namespace LinKb.Core;

public struct PadStatus
{
    public bool IsPressed => Velocity01 > 0;
    public float Velocity01;
    public Vector3 Axes;
}


public readonly struct ReadOnly<T> where T : struct
{
    public readonly T Status;
    public ReadOnly(in T status) => Status = status;
    
    public static implicit operator ReadOnly<T>(in T status) => new(status);
}