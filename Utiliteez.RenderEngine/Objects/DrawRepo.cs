using System.Numerics;
using Utiliteez.RenderEngine.Structs;

namespace Utiliteez.RenderEngine.Objects;

public class DrawRepo
{
    private List<Material> _materials = new();
    private List<uint> _indices = new();
    private List<Vertex> _vertices = new();


    public Vertex[] Vertices => _vertices.ToArray();
    public uint[] Indices => _indices.ToArray();
    public Material[] Materials => _materials.ToArray();


    public void AddModel(Model model, Vector3 position)
    {
        var idxOffset = (uint) _vertices.Count;
        _materials.AddRange(model.Materials);
        
        _vertices.AddRange(model.Vertices.Select(v =>
        {
            v.Position += position;
            return v;
        }));
        
        _indices.AddRange(model.Indices.Select(i => i + idxOffset));
    }
}