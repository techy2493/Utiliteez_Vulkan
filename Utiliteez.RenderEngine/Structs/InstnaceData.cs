using System.Numerics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit, Size = 80)]
public struct InstanceData
{
    [FieldOffset(0)]  public Matrix4x4 Model;          // 64 bytes
    [FieldOffset(64)] public uint MaterialIndex;
    [FieldOffset(68)] private uint _pad0; 
    [FieldOffset(72)] private uint _pad1; 
    [FieldOffset(76)] private uint _pad2;     // pad to 16B alignment
}