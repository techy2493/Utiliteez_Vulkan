using System.Numerics;
using Assimp;
using Utiliteez.RenderEngine.Structs;
using Material = Utiliteez.RenderEngine.Structs.Material;

namespace Utiliteez.RenderEngine;

public static class AssimpExtension
{
    public static Vector3 ToVector3(this Color4D c)
        => new(c.R, c.G, c.B);
    
    public static Vector3 ToVector3(this Assimp.Vector3D c)
        => new(c.X, c.Y, c.Z);

    public static Vector2 ToVector2(this Assimp.Vector2D c)
        => new(c.X, c.Y);
    
    
    public static Vector2 ToVector2(this Assimp.Vector3D c)
        => new(c.X, c.Y);


    public static Material ToMaterialEntry(this Assimp.Material material)
    {
        return new Material(material.ColorDiffuse.ToVector3(), material.TextureDiffuse.FilePath);
    }
}