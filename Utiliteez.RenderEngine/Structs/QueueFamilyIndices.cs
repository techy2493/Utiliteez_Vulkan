namespace Utiliteez.RenderEngine.Structs;

public record struct QueueFamilyIndices
{
    public uint? GraphicsFamily { get; set; }
    public uint? PresentFamily { get; set; }
    public bool IsComplete()
    {
        return GraphicsFamily.HasValue && PresentFamily.HasValue;
    }
        
    public uint[] ToDistinctArray()
    {
        return new []{GraphicsFamily!.Value, PresentFamily!.Value}.Distinct().ToArray();
        // [0]
    }
}