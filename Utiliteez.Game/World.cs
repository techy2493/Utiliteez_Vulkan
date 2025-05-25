using System.Numerics;
using Utiliteez.Game;

namespace Utiliteez.RenderEngine.Objects;

public class World
{
    
    private Client _client;
    private Scene _scene;
    public uint Height { get; }
    public uint Width { get; }
    public World(Client client, uint height, uint width)
    {
        Height = height;
        Width = width;
        _client = client;
        EntityPositions = new SpatialIndex();
    }
    
    public EntityIdPool EntityIdPool { get; set; }
    public SpatialIndex EntityPositions { get; set; }
    public Dictionary<Type, object> EntityPools { get; set; }
    public Dictionary<Type, object> NetworkPools { get; set; }

    private EntityPool<T> GetEntityPool<T>() where T : Entity, new()
    {
        EntityPools.TryGetValue(typeof(T), out var pool);
        return (EntityPool<T>)pool ?? EntityPool<T>.CreateEntityPool(this, _client);
    }
    
    public int AddEntity<T>(T entity, Vector2 position) where T: Entity, new()
    {
        var id = EntityIdPool.Get();
        entity.Init(id, this, null);
        EntityPositions.SetPosition(entity.Id, position);
        GetEntityPool<T>().RegisterEntity(entity);
        return id;
    }
    
    public T GetEntity<T>(int entityId) where T : Entity, new()
    {
        var pool = GetEntityPool<T>();
        if (pool.isType(entityId))
        {
            return pool.Get(entityId);
        }
        else
        {
            throw new Exception($"Entity with ID {entityId} not found in pool.");
        }
    }
}