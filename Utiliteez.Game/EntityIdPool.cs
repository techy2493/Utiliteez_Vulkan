namespace Utiliteez.RenderEngine.Objects;

public class EntityIdPool
{
    private Queue<int> _queue;
    private HashSet<int> _set; // prevent duplicates
    private int _maxId = 10000;
    private int _nextNewId = 0;

    public EntityIdPool()
    {
        _queue = new Queue<int>();
        _set = new HashSet<int>();
    }

    public int Get()
    {
        if (_queue.Count == 0)
        {
            return _nextNewId++;
        }
        var id = _queue.Dequeue();
        _set.Remove(id);
        return id;
    }

    public void Free(int id)
    {
        if (_set.Add(id))
        {
            _queue.Enqueue(id);
        }
    }
}