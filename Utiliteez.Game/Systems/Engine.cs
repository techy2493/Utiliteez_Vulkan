namespace Utiliteez.Game.Systems;

public record EngineEvent(
    int SenderId
);

public abstract class Engine<T> where T: EngineEvent
{
    private Queue<T> _queue = new();
    
    public void AddEvent(T systemEvent)
    {
        _queue.Enqueue(systemEvent);
    }
}