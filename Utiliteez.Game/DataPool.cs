using Utiliteez.RenderEngine.Objects;

namespace Utiliteez.Game;

public class DataPool<T> where T : Entity, new()
{
    private Dictionary<string, T>[] _data;
    private uint _maxEntities = 100;
    private EntityPool<T> _entityPool;
    
    public DataPool()
    {
        
    }
}