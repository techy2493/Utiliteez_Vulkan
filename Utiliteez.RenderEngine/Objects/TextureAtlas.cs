using System.Numerics;
using RectpackSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Utiliteez.RenderEngine.Structs;

namespace Utiliteez.RenderEngine.Objects;

public class TextureAtlas
{
    
    // todo make this shit not static
    public static Image<Rgba32> Atlas = new (1, 1);
    
    public static void Generate(Material[] materials)
    {
        if (materials.Length == 0)
            return;
        
        Image[] textures = new Image[materials.Length];
        // 1) Get Used Area Rect of each Material
        PackingRectangle[] matRects = new PackingRectangle[materials.Length];
        for (var i = 0; i < materials.Length; i++)
        {
            textures[i] = Image.Load<Rgba32>(materials[i].TexturePath);
            var rect = new PackingRectangle(new System.Drawing.Rectangle
            {
                Width = textures[i].Width,
                Height = textures[i].Height
            }, i);
            matRects[i] = rect;
        }
        
        // 2) Get packed Rects (Force Bottom Left origin) ??
        RectanglePacker.Pack(matRects, out var atlasBounds, PackingHints.MostlySquared);
    
        // 3.-1) Create Atlas Image
        Atlas = new Image<Rgba32>((int)atlasBounds.Width, (int)atlasBounds.Height);
        
        // 3) Iterate Image & Rects Inserting only used portion of original texture into atlas at rect
        for (var i = 0; i < matRects.Length; i++)
        {
            Atlas.Mutate(ctx => 
                ctx.DrawImage(textures[i], new Point((int)matRects[i].X, (int)matRects[i].Y), 1f)
            );
        }
        
        Atlas.Save("atlas.png");
        
        // 5) Now update each Material.UvOffset/AtlasScale based on its packRect
        for (var i = 0; i < materials.Length; i++)
        {
            var mat = materials[i];
            var rect = matRects[i];
            if (!mat.HasTexture) continue;
            
            var pixelRect = new Rectangle(
                (int)rect.X, (int)rect.Y,
                (int)rect.Width, (int)rect.Height
            );
        
            mat.FinalizeAtlas(pixelRect, new Vector2(Atlas.Width, Atlas.Height));
        }
    }
    
}