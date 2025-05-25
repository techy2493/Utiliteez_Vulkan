using Utiliteez.Render;
using Utiliteez.RenderEngine.Structs;

namespace Utiliteez.RenderEngine;

public class ResourceRepository<T>(ILoader<T> loader)
    where T : new()
{
    private readonly Dictionary<string, T> _cache = new();

    public T Request(string path)
    {
        if (!_cache.TryGetValue(path, out var item))
        {
            item = loader.Load(path, (uint)_cache.Count);
            _cache[path] = item;
        }
        return item;
    }
    
    public T[] GetAll()
    {
        return _cache.Values.ToArray();
    }
    
}