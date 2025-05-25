using System.Numerics;

namespace Utiliteez.RenderEngine.Structs;

public struct UniformBufferObject
{
    public Matrix4x4 model;
    public Matrix4x4 view;
    public Matrix4x4 proj;
}