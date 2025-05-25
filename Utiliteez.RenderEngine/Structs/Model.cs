namespace Utiliteez.RenderEngine.Structs;

public readonly struct Model
{
    public Vertex[] Vertices { get; init; }
    public uint[] Indices { get; init; }
    public Material[] Materials { get; init; }
}