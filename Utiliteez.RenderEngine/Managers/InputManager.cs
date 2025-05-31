using System.Numerics;
using System.Runtime.InteropServices.JavaScript;
using Silk.NET.Input;

namespace Utiliteez.RenderEngine;

public class InputManager(ITimingManager TimingManager, IWindowManager WindowManager) : IInputManager
{
    public event Action<KeyEventArgs>? OnKeyPressed;
    public event Action<KeyEventArgs>? OnKeyDown;
    public event Action<KeyEventArgs>? OnKeyUp;
    public event Action<MouseEventArgs>? OnMouseButtonPressed;
    public event Action<MouseEventArgs>? OnMouseButtonDown;
    public event Action<MouseEventArgs>? OnMouseButtonUp;
    public event Action<MouseEventArgs>? OnMouseMoved;

    private readonly double[] _keysDown        = new double[Enum.GetValues<Keycode>().Length];
    private readonly double[] _keysUp          = new double[Enum.GetValues<Keycode>().Length];
    private readonly double[] _mouseButtonsDown       = new double[Enum.GetValues<MouseButton>().Length];
    private readonly double[] _mouseButtonsUp         = new double[Enum.GetValues<MouseButton>().Length];

    // public read-only views
    public IReadOnlyList<double> KeysDown         => _keysDown;
    public IReadOnlyList<double> KeysUp           => _keysUp;
    public IReadOnlyList<double> MouseButtonsDown => _mouseButtonsDown;
    public IReadOnlyList<double> MouseButtonsUp   => _mouseButtonsUp;

    public ICameraManager CameraManager { get; private set; }
    
    
    public void BindCamera(ICameraManager cameraManager)
    {
        CameraManager = cameraManager;
    }
    
    
    public void Initialize()
    {
        var input = WindowManager.Window.CreateInput();
        foreach (var keyboard in input.Keyboards)
        {
            // TODO: Handle key codes properly, this is a workaround. Try a map of some sort?
            keyboard.KeyDown += (_, key, _) => KeyDown(new KeyEventArgs(Enum.Parse<Keycode>(key.ToString())));
            keyboard.KeyUp += (_, key, _) => KeyUp(new KeyEventArgs(Enum.Parse<Keycode>(key.ToString())));
        }

        foreach (var mouse in input.Mice)
        {
            // TODO: Handle key codes properly, this is a workaround. Try a map of some sort?
            mouse.MouseDown += (m, button) => MouseButtonDown(new MouseEventArgs(
                Enum.Parse<MouseButton>(button.ToString()),
                m.Position.X, m.Position.Y));
            mouse.MouseUp += (m, button) => MouseButtonUp(new MouseEventArgs(
                Enum.Parse<MouseButton>(button.ToString()),
                m.Position.X, m.Position.Y));
        }
    }
    
    public void KeyDown(KeyEventArgs e)
    {
        var eventArgs = e with
        {
            TimeSinceLastPressed = TimingManager.Now - KeysUp[(int)e.Key]
        };
        _keysDown[(int)e.Key] = TimingManager.Now;
        _keysUp[(int)e.Key] = long.MinValue;
        OnKeyDown?.Invoke(eventArgs);
    }
    
    public void KeyUp(KeyEventArgs e)
    {
        var eventArgs = e with
        {
            TimeHeld = TimingManager.Now - KeysDown[(int)e.Key]
        };
        _keysDown[(int)e.Key] = long.MinValue;
        _keysUp[(int)e.Key] = TimingManager.Now;
        OnKeyUp?.Invoke(eventArgs);
        if (eventArgs.TimeHeld > 0)
            OnKeyPressed?.Invoke(eventArgs);
    }
    public void MouseButtonDown(MouseEventArgs e)
    {
        var eventArgs = e with
        {
            TimeSinceLastPressed = TimingManager.Now - MouseButtonsUp[(int)e.Button],
            WorldPosition = CameraManager.ScreenToGround(e.X, e.Y) ?? Vector3.Zero
        };
        _mouseButtonsUp[(int)e.Button] = long.MinValue;
        _mouseButtonsDown[(int)e.Button] = TimingManager.Now;
        OnMouseButtonDown?.Invoke(eventArgs);
    }
    public void MouseButtonUp(MouseEventArgs e)
    {
        var eventArgs = e with
        {
            TimeHeld = TimingManager.Now - MouseButtonsDown[(int)e.Button],
            WorldPosition = CameraManager.ScreenToGround(e.X, e.Y) ?? Vector3.Zero
        };
        _mouseButtonsDown[(int)e.Button] = long.MinValue;
        _mouseButtonsUp[(int)e.Button] = TimingManager.Now;
        OnMouseButtonUp?.Invoke(eventArgs);
        if (eventArgs.TimeHeld > 0)
            OnMouseButtonPressed?.Invoke(eventArgs);
    }
    public void MouseMoved(MouseEventArgs e)
    {
        OnMouseMoved?.Invoke(e);
    }
}

public readonly struct KeyEventArgs(Keycode key)
{
    public Keycode Key { get; init; } = key;
    public double? TimeHeld { get; init; } = null;
    public double? TimeSinceLastPressed { get; init; } = null;
}

public struct MouseEventArgs(MouseButton? button, float x, float y)
{
    public MouseButton? Button { get; init; } = button;
    public float X { get; init; } = x;
    public float Y { get; init; } = y;
    public double? TimeHeld { get; init; } = null;
    public double? TimeSinceLastPressed { get; init; } = null;
    public Vector3 WorldPosition;
}

public enum MouseButton
{
    Left=0,Right,Middle,ScrollUp,ScrollDown,Mouse3,Mouse4,Mouse5,Mouse6,Mouse7,Mouse8
}

public enum Keycode
{
    A=0,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,R,S,T,U,V,W,X,Y,Z,
    Num0, Num1, Num2, Num3, Num4, Num5, Num6, Num7, Num8, Num9,
    Escape, Enter, Space, Backspace, Tab, Shift, Control, Alt,
    Up, Down, Left, Right,
    PageUp, PageDown, Home, End,
    Insert, Delete,
    CapsLock, NumLock, ScrollLock,
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
    PrintScreen, Pause,
    Unknown
}