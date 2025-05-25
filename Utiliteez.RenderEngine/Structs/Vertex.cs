using System.Numerics;
using System.Runtime.InteropServices;

namespace Utiliteez.RenderEngine.Structs;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex
{
    public Vector3 Position;
    public Vector3 Normal;
    public Vector2 Uv;
    public UInt32 MaterialIndex;
}