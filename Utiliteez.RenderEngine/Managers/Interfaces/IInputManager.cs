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
    IReadOnlyList<double> KeysDown { get; }
    IReadOnlyList<double> KeysUp { get; }
    IReadOnlyList<double> MouseButtonsDown { get; }
    IReadOnlyList<double> MouseButtonsUp { get; }
    void KeyDown(KeyEventArgs e);
    void KeyUp(KeyEventArgs e);
    void MouseButtonDown(MouseEventArgs e);
    void MouseButtonUp(MouseEventArgs e);
    void MouseMoved(MouseEventArgs e);
    public void Initialize();
    public void BindCamera(ICameraManager cameraManager);
}