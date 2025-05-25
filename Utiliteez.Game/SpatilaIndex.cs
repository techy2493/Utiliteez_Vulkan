using System.Numerics;

public class SpatialIndex
{
    // fast Entity → Position
    Dictionary<int, Vector2> entityToPos = new();

    // fast Position → Entity (or Entities, if multiple can share a cell)
    Dictionary<Vector2, HashSet<int>> posToEntities = new(
        // IMPORTANT: you’ll need an IEqualityComparer<Vector2> that handles floats
        // or switch to an integer‐grid type like Point or Int2.
        new Vector2EqualityComparer()
    );

    public void SetPosition(int entityId, Vector2 newPos)
    {
        // 1) Remove old mapping
        if (entityToPos.TryGetValue(entityId, out var oldPos))
        {
            if (posToEntities.TryGetValue(oldPos, out var set))
            {
                set.Remove(entityId);
                if (set.Count == 0) posToEntities.Remove(oldPos);
            }
        }

        // 2) Update forward map
        entityToPos[entityId] = newPos;

        // 3) Update reverse map
        if (!posToEntities.TryGetValue(newPos, out var ents))
        {
            ents = new HashSet<int>();
            posToEntities[newPos] = ents;
        }
        ents.Add(entityId);
    }

    public Vector2 GetPosition(int entityId)
    {
        return entityToPos.TryGetValue(entityId, out var pos)
            ? pos
            : throw new KeyNotFoundException($"No position for {entityId}");
    }

    public IReadOnlyCollection<int> GetEntitiesAt(Vector2 pos)
    {
        return posToEntities.TryGetValue(pos, out var ents)
            ? ents
            : Array.Empty<int>();
    }

    public void RemoveEntity(int entityId)
    {
        if (entityToPos.TryGetValue(entityId, out var pos))
        {
            entityToPos.Remove(entityId);
            var set = posToEntities[pos];
            set.Remove(entityId);
            if (set.Count == 0) posToEntities.Remove(pos);
        }
    }
}

// Simple float‐tolerant comparer (if you really need exact floats),
// or better: use integer‐based grid keys.
class Vector2EqualityComparer : IEqualityComparer<Vector2>
{
    public bool Equals(Vector2 a, Vector2 b)
        => MathF.Abs(a.X - b.X) < 0.0001f
        && MathF.Abs(a.Y - b.Y) < 0.0001f;

    public int GetHashCode(Vector2 v)
        => HashCode.Combine(
            MathF.Round(v.X, 4),
            MathF.Round(v.Y, 4)
        );
}