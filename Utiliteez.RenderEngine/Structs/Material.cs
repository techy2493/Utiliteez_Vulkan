using Assimp;
using SixLabors.ImageSharp;

namespace Utiliteez.RenderEngine.Structs;

using System.Numerics;
using System.Runtime.InteropServices;

public class Material : IEquatable<Material>
{
    public readonly Vector3 Color;
    public readonly string TexturePath;
    public bool HasTexture => !string.IsNullOrEmpty(TexturePath);
    public Vector2 UvOffset;
    public Vector2 AtlasScale;
    public float UMax, VMax, UMin, VMin;
    //
    public Material(Vector3 c, string path)
    {
        Color       = c;
        TexturePath = path;
        UvOffset    = Vector2.Zero;
        AtlasScale  = Vector2.Zero;
        UMin = VMin = float.MaxValue;
        UMax = VMax = float.MinValue;
     }

    public void UpdateRect(Vector2 rawUv)
    {
        UMax = MathF.Max(rawUv.X, UMax);
        VMax = MathF.Max(rawUv.Y, VMax);
        UMin = MathF.Min(rawUv.X, UMin);
        VMin = MathF.Min(rawUv.Y, VMin);
    }
    
    /// <summary>
    /// pixelRect: the rectangle of the sliced (and scaled) region within the atlas
    /// atlasSizePx: the atlas dimensions (width, height)
    /// </summary>
    public void FinalizeAtlas(Rectangle pixelRect, Vector2 atlasSizePx)
    {
        
        var upscale = 8f;
        
        if (!HasTexture) return;
        // 1) How big our up-scaled slice really is, in atlas UVs
        float tileWuv = (pixelRect.Width  /* already upscaled */) / atlasSizePx.X;
        float tileHuv = (pixelRect.Height /* already upscaled */) / atlasSizePx.Y;

        // 2) Where its bottom-left landed, in atlas UVs
        float baseU = pixelRect.X / atlasSizePx.X;
        float baseV = pixelRect.Y / atlasSizePx.Y;
        
        // float baseV = (atlasSizePx.Y 
        //                - pixelRect.Y 
        //                - pixelRect.Height)
        //               / atlasSizePx.Y;
        Console.WriteLine($"tile at Y={pixelRect.Y} gives baseV={baseV:F3}");

        // 4) Final
        AtlasScale = new Vector2(tileWuv, tileHuv);
        UvOffset   = new Vector2(baseU, baseV);
        
        //
        // float scaleX = pixelRect.Width  / atlasSizePx.X;
        // float scaleY = pixelRect.Height / atlasSizePx.Y;
        //
        // // 2) where its BOTTOM-LEFT landed in atlas UV‐space
        // float baseX  = pixelRect.X / atlasSizePx.X;
        // float baseY  = pixelRect.Y / atlasSizePx.Y;
        //
        // // 3) how many atlas‐UVs we must back up
        // //    because we CROPPED away UMin…VMin *before* upscaling
        // float cropX  = (UMin * originalTextureSizePx.X * upscale) / atlasSizePx.X;
        // float cropY  = (VMin * originalTextureSizePx.Y * upscale) / atlasSizePx.Y;
        //
        // // 4) final
        // AtlasScale  = new Vector2(scaleX, scaleY);
        // UvOffset    = new Vector2(baseX - cropX,
        //     baseY - cropY);
        
//         // 1) How big our tile is in atlas UV‐space:
//         var atlasScaleX = pixelRect.Width  / atlasSizePx.X;
//         var atlasScaleY = pixelRect.Height / atlasSizePx.Y;
//
// // 2) Where the tile’s bottom-left landed, in [0…1]:
//         var placeX = pixelRect.X / atlasSizePx.X;
// //  because cropping was top-origin, flip Y:
//         var placeY = 1 - (pixelRect.Y / atlasSizePx.Y) - atlasScaleY;
//
// // 3) How far “into” that tile we must back up 
// //    because we chopped off UMin…VMin at the start:
// //    (UMin/(UMax−UMin)) tells us the fraction into the tile 
// //    where the model’s UV=0 begins.
//         var cropOffsetX = (UMin / (UMax - UMin)) * atlasScaleX;
//         var cropOffsetY = (VMin / (VMax - VMin)) * atlasScaleY;
//
// // 4) Final UV parameters:
//         AtlasScale = new Vector2(atlasScaleX, atlasScaleY);
//         UvOffset   = new Vector2(placeX - cropOffsetX,
//             placeY - cropOffsetY);
        // // 1) How big the tile ended up in your atlas, normalized [0…1]:
        // float tileScaleX = pixelRect.Width  / atlasSizePx.X;
        // float tileScaleY = pixelRect.Height / atlasSizePx.Y;
        //
        // // 2) Where that tile starts in your atlas, normalized [0…1]:
        // float placeX = pixelRect.X / atlasSizePx.X;
        // float placeY = pixelRect.Y / atlasSizePx.Y;
        //
        // // 3) Where you *originally* cropped the image, in atlas pixels:
        // //    UMin (0…1) * original width * upscale → pixels of the crop origin
        // float cropPxX = UMin * originalTextureSizePx.X;
        // float cropPxY = VMin * originalTextureSizePx.Y;
        //
        // // 4) Normalize that crop offset against the atlas
        // float cropOffsetX = (cropPxX / atlasSizePx.X)  * upscale;
        // float cropOffsetY = (cropPxY / atlasSizePx.Y) * upscale;
        //
        // AtlasScale = new Vector2(tileScaleX, tileScaleY);
        // UvOffset   = new Vector2(placeX - cropOffsetX,
        //     placeY - cropOffsetY);
        
// //         // how big the tile is, in [0…1]
//         var tileScaleX = pixelRect.Width  / originalTextureSizePx.X;
//         var tileScaleY = pixelRect.Height / originalTextureSizePx.Y;
//
// // where it got placed, in [0…1]
//         var placeX = pixelRect.X / atlasSizePx.X;
//         var placeY = pixelRect.Y / atlasSizePx.Y;
//
// // now *undo* the crop you did originally at UMin,VMin:
//         var cropOffsetX = UMin * tileScaleX;
//         var cropOffsetY = VMin * tileScaleY;
//
// // final
//         AtlasScale = new Vector2(tileScaleX, tileScaleY);
//         UvOffset   = new Vector2(placeX - cropOffsetX,
//             placeY - cropOffsetY);
        
        // AtlasScale  = new Vector2(
        //     pixelRect.Width  / atlasSizePx.X,
        //     pixelRect.Height / atlasSizePx.Y
        // );
        // UvOffset = new Vector2(
        //     pixelRect.X       / atlasSizePx.X,
        //     pixelRect.Y       / atlasSizePx.Y
        // );
        
        // AtlasScale = new Vector2(
        //     pixelRect.Width / atlasSizePx.X,
        //     pixelRect.Height / atlasSizePx.Y);
        //
        // UvOffset = new Vector2(
        //     (pixelRect.X * upscale) / atlasSizePx.X - (UMin * upscale) * (1 / atlasSizePx.X),
        //     (pixelRect.Y * upscale) / atlasSizePx.Y - (VMin * upscale) * (1 / atlasSizePx.Y));
        //
        // // 1) How much of the atlas corresponds to one pixel in the *original* texture
        // AtlasScale = new Vector2(
        //     originalTextureSizePx.X * 0.5f / atlasSizePx.X,
        //     originalTextureSizePx.Y * 0.5f / atlasSizePx.Y
        // );
        //
        // // 2) Shift into atlas-UV so that rawUv == UMin maps to pixelRect.X,
        // //    i.e. subtract off the UMin portion of the original texture space
        // UvOffset = new Vector2(
        //     (pixelRect.X + 8f - UMin * originalTextureSizePx.X) / atlasSizePx.X,
        //     (pixelRect.Y - 8f - VMin * originalTextureSizePx.Y) / atlasSizePx.Y
        // );
    }


    public bool Equals(Material other)
        => Color.Equals(other.Color)
           && TexturePath == other.TexturePath;

    public override int GetHashCode()
        => HashCode.Combine(Color, TexturePath);
    
    public static explicit operator ShaderMaterial(Material entry)
        => new ShaderMaterial(
            entry.Color,
            entry.UvOffset,
            entry.AtlasScale
        ); 
}

[StructLayout(LayoutKind.Explicit, Size = 32)]
public readonly struct ShaderMaterial
{
    [FieldOffset(0)]  public readonly Vector3 Color;
    [FieldOffset(16)] public readonly Vector2 UvOffset;
    [FieldOffset(24)] public readonly Vector2 AtlasScale;

    public ShaderMaterial(Vector3 color, Vector2 uvOffset, Vector2 atlasScale)
    {
        Color      = color;
        UvOffset   = uvOffset;
        AtlasScale = atlasScale;
    }
    
    public static void VerifyLayout()
    {
        Console.WriteLine("Sizeof(ShaderMaterial) = " + Marshal.SizeOf<ShaderMaterial>() + " bytes");
        Console.WriteLine("UvOffset @ " + Marshal.OffsetOf<ShaderMaterial>("UvOffset"));
        Console.WriteLine("AtlasScale @ " + Marshal.OffsetOf<ShaderMaterial>("AtlasScale"));
    }
}
