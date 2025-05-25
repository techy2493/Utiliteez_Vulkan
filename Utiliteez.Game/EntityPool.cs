using System.Collections;
using Utiliteez.Game;

namespace Utiliteez.RenderEngine.Objects;

public class EntityPool<T> : IEnumerable<T> where T : Entity, new()
{
    private World _world;
    private Client _client;

    List<T> Entities { get; } = new();
    Dictionary<int, int> EntityIdToIndex { get; } = new();

    public EntityPool(World world, Client client)
    {
        _world = world;
        _client = client;
    }

    public void RegisterEntity(T entity)
    {
        // 1) compute the slot you’ll use
        int index = Entities.Count;

        // 2) add the component to the dense array
        Entities.Add(entity);

        // 3) record the mapping from the external ID to that slot
        EntityIdToIndex.Add(entity.Id, index);
    }

    public T Get(int entityId)
    {
        if (EntityIdToIndex.TryGetValue(entityId, out var index))
        {
            return Entities[index];
        }
        else
        {
            throw new Exception($"Entity with ID {entityId} not found in pool.");
        }
    }

    public bool isType(int entityId)
    {
        return EntityIdToIndex.ContainsKey(entityId);
    }

    public void RemoveEntity(int entityId)
    {
        if (!EntityIdToIndex.TryGetValue(entityId, out var index))
            throw new Exception($"Entity {entityId} not in pool.");

        int last = Entities.Count - 1;

        // 1) Move last element into the hole, if it's not the same one
        if (index != last)
        {
            Entities[index] = Entities[last];
            // 2) Update the moved entity's map entry
            EntityIdToIndex[Entities[index].Id] = index;
        }

        // 3) Pop the now‐duplicate last slot
        Entities.RemoveAt(last);

        // 4) Finally remove the deleted entity from the map
        EntityIdToIndex.Remove(entityId);
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < Entities.Count; i++)
        {
            yield return Entities[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public static EntityPool<T> CreateEntityPool(World world, Client client)
    {
        var pool = new EntityPool<T>(world, client);
        world.EntityPools.Add(typeof(T), pool);
        return pool;
    }
}