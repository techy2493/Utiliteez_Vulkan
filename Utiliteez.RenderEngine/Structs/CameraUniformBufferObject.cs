using System.Numerics;

namespace Utiliteez.RenderEngine.Structs;

public struct CameraUniformBufferObject
{
    public Matrix4x4 view;
    public Matrix4x4 proj;
}