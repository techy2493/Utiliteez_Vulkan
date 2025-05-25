namespace Utiliteez.RenderEngine.Structs;

public readonly struct Model
{
    public Model(
        Int32 vertexOffset,
        uint indexOffset,
        uint indexCount,
        uint materialIndex)
    {
        VertexOffset = vertexOffset;
        IndexOffset = indexOffset;
        IndexCount = indexCount;
        MaterialIndex = materialIndex;
    }
    public readonly Int32 VertexOffset;
    public readonly UInt32 IndexOffset;
    public readonly UInt32 IndexCount;
    public readonly UInt32 MaterialIndex;
}