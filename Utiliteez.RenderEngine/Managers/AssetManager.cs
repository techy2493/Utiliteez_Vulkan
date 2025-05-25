using System.Numerics;
using Utiliteez.Render;
using Utiliteez.RenderEngine.Structs;

namespace Utiliteez.RenderEngine;

public class AssetManager : IAssetManager
{
    readonly ModelLoader _modelLoader = new();
    readonly Dictionary<string, Model> _models = new();

    public List<uint> Indices { get; } = new();
    public List<Vertex> Vertices { get; } = new();
    public List<Font> Fonts { get; } = new();
    public List<Material> Materials { get; } = new();

    public Dictionary<string, Model> Models => _models;

    public void RegisterModel(string id, string filePath)
    {
        var (vertices, indices, material) = _modelLoader.Load(filePath);
        var model = new Model(
            Vertices.Count,
            (uint)Indices.Count,
            (uint)indices.Length,
            RegisterMaterial(material)
        );
        Vertices.AddRange(vertices);
        Indices.AddRange(indices);
        _models.Add(id, model);
    }
    public void RegisterFont(Font font)
    {
        Fonts.Add(font);
    }

    public uint RegisterMaterial(Material material)
    {
        Materials.Add(material);
        return (uint) Materials.Count - 1;
    }

    public void Reset()
    {
        Models.Clear();
        Vertices.Clear();
        Indices.Clear();
        Materials.Clear();
        Fonts.Clear();
    }
    
    
}