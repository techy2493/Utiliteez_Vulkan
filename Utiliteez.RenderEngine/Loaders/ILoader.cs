namespace Utiliteez.Render;

public interface ILoader<T>
{
    public T Load(string path, uint matOffset = 0);
}