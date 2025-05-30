using Utiliteez.RenderEngine.Structs;

namespace Utiliteez.RenderEngine;

public interface IAssetManager
{
    void RegisterModel(string id, string filePath);
    void RegisterFont(Font font);
    uint RegisterMaterial(Material material);
    void Reset();
    public Dictionary<string, Model> Models { get; }
    public List<Material> Materials { get; }
    public List<Font> Fonts { get; }
    public List<Vertex> Vertices { get ;}
    public List<uint> Indices { get;  }
    void Finalize();
}