using System.Numerics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit, Size = 80)]
public struct InstanceData
{
    [FieldOffset(0)]  public Matrix4x4 Model;          // 64 bytes
    [FieldOffset(64)] public uint MaterialIndex;
    [FieldOffset(68)] private Vector3 _pad;            // pad to 16B alignment
}