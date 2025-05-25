using System.Numerics;
using System.Runtime.InteropServices;

namespace Utiliteez.RenderEngine.Structs;

[StructLayout(LayoutKind.Explicit, Size = 48)]
public struct Vertex
{
    [FieldOffset(0)]
    public Vector3 Position;       // 0–11

    [FieldOffset(12)]
    private float _pad0;           // 12–15: padding to align next field

    [FieldOffset(16)]
    public Vector3 Normal;         // 16–27

    [FieldOffset(28)]
    private float _pad1;           // 28–31: padding to align next field

    [FieldOffset(32)]
    public Vector2 Uv;             // 32–39

    [FieldOffset(40)]
    private Vector2 _pad2;         // 40–47: final padding for 16-byte stride
}