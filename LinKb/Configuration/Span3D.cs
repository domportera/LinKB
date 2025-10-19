using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LinKb.Configuration;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct Span3D<T>
{
    private readonly T[,,] _data;
    private readonly Vector<int> _size;
    private readonly Vector<int> _offset;
    public int XLength => _size[0];
    public int YLength => _size[1];
    public int ZLength => _size[2];
    public int Length => _size[0] * _size[1] * _size[2];
    
    public Span3D(T[,,] data)
    {
        _size = new Vector<int>([data.GetLength(0), data.GetLength(1), data.GetLength(2)]);
        _offset = new Vector<int>([0, 0, 0]);
        _data = data;
    }
    
    public Span3D(in T[,,] data, int offsetX, int lengthX, int offsetY, int lengthY, int offsetZ, int lengthZ)
    {
#if DEBUG
        Debug.Assert(data.Rank == 2);
        var arrayEnd = new Vector<int>([data.GetLength(0), data.GetLength(1), data.GetLength(2)]);
        var boundsEnd = new Vector<int>([offsetX + lengthX, offsetY + lengthY, offsetZ + lengthZ]);
        
        // assert all(arrayEnd >= boundsEnd)
        Debug.Assert(Vector.GreaterThanOrEqual(arrayEnd, boundsEnd) == new Vector<int>([1, 1, 1]));
#endif
        _size = new Vector<int>([lengthX, lengthY, lengthZ]);
        _offset = new Vector<int>([offsetX, offsetY, offsetZ]);
        _data = data;
    }

    private Span3D(Span3D<T> other, int offsetX, int lengthX, int offsetY, int lengthY, int offsetZ, int lengthZ)
    {
        _size = new Vector<int>([lengthX, lengthY, lengthZ]);
        _offset = new Vector<int>([offsetX, offsetY, offsetZ]) + other._offset;
        _data = other._data;
    }

    // array accessors
    public T this[int x, int y, int z]
    {
        get => _data[x, y, z];
        set => _data[x, y, z] = value;
    }
    
    // todo: range accessors - can we do this in 1d, 2d, and 3d?
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Span3D<T>(T[,,] data) => new(data);
    
    // implicit conversion to readonlySpan
    //todo: remove vector accessors - vectorization should be utilized externally, not internally
    public static implicit operator ReadOnlySpan3D<T>(Span3D<T> span) => new(span._data, span._offset[0], span._size[0], span._offset[1], span._size[1], span._offset[2], span._size[2]);
}

internal static class Span3DExtensions
{
    public static Span3D<T> AsSpan3D<T>(this T[,,] array) => new(array);
}