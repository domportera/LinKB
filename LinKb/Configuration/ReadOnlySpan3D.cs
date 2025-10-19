using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LinKb.Configuration;

// todo - source generator to generate N-dimensional spans? is this just what tensors are?
[StructLayout(LayoutKind.Sequential)]
public readonly ref struct ReadOnlySpan3D<T>
{
    private readonly T[,,] _data;

    private readonly int _sizeX, _sizeY, _sizeZ, _offsetX, _offsetY, _offsetZ;

    internal ReadOnlySpan3D(T[,,] data, int offsetX, int lengthX, int offsetY, int lengthY, int offsetZ, int lengthZ)
    {
        Debug.Assert(offsetX + lengthX <= data.GetLength(0));
        Debug.Assert(offsetY + lengthY <= data.GetLength(1));
        Debug.Assert(offsetZ + lengthZ <= data.GetLength(2));
        _sizeX = lengthX;
        _sizeY = lengthY;
        _sizeZ = lengthZ;
        _offsetX = offsetX;
        _offsetY = offsetY;
        _offsetZ = offsetZ;
        _data = data;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan3D(T[,,] keymap) : this(keymap, 0, keymap.GetLength(0), 0, keymap.GetLength(1), 0, keymap.GetLength(2))
    {
    }

    
    public int Length => Size[0] * Size[1] * Size[3];
    public int Volume => Length;
    public Vector<int> Size => Vector.Create([_sizeX, _sizeY, _sizeZ, 0]);
    public Vector<int> Offset => Vector.Create<int>([_offsetX, _offsetY, _offsetZ, 0]);
    public int XLength => _sizeX;
    public int YLength => _sizeY;
    public int ZLength => _sizeZ;
    public int XOffset => _offsetX;
    public int YOffset => _offsetY;
    public int ZOffset => _offsetZ;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlySpan3D<T>(T[,,] data)
    {
        return new ReadOnlySpan3D<T>(data: data, 
            offsetX: 0, 
            lengthX: data.GetLength(0), 
            offsetY: 0, 
            lengthY: data.GetLength(1), 
            offsetZ: 0, 
            lengthZ: data.GetLength(2));
    }

    internal ref readonly T this[int x, int y, int z]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _data[x, y, z];
    }

    public T[,,] ToArray()
    {
        var arr = new T[XLength, YLength, ZLength];
        Array.Copy(_data, arr, _data.Length);
        return arr;
    }
}