namespace Utiliteez.RenderEngine;

public interface IInputManager
{
    event Action<KeyEventArgs>? OnKeyPressed;
    event Action<KeyEventArgs>? OnKeyDown;
    event Action<KeyEventArgs>? OnKeyUp;
    event Action<MouseEventArgs>? OnMouseButtonPressed;
    event Action<MouseEventArgs>? OnMouseButtonDown;
    event Action<MouseEventArgs>? OnMouseButtonUp;
    event Action<MouseEventArgs>? OnMouseMoved;
    IReadOnlyList<long> KeysDown { get; }
    IReadOnlyList<long> KeysUp { get; }
    IReadOnlyList<long> MouseButtonsDown { get; }
    IReadOnlyList<long> MouseButtonsUp { get; }
    void KeyDown(KeyEventArgs e);
    void KeyUp(KeyEventArgs e);
    void MouseButtonDown(MouseEventArgs e);
    void MouseButtonUp(MouseEventArgs e);
    void MouseMoved(MouseEventArgs e);
}