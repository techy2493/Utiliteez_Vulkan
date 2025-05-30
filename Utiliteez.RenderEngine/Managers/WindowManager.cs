using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Utiliteez.RenderEngine;
// Todo: Move this shit?? IWindow in common? Window in Game? Or just expose an event sink?
public class WindowManager(IInputManager InputManager) : IWindowManager
{
    public readonly Vector2D<int> WindowSize = new Vector2D<int>(800, 600);
    public const string Title = "Utiliteez";

    public IWindow Window { get; private set; }
    
    public void CreateWindow()
    {
        var opts = WindowOptions.Default with
        {
            Size = WindowSize,
            Title = Title,
            API = GraphicsAPI.DefaultVulkan,
            
        };
        
        Window = Silk.NET.Windowing.Window.Create(opts);

        Window.Load += OnLoad;
        Window.Update += OnUpdate;
        Window.Render += OnRender;
        
        Window.Initialize();
        
        if (Window.VkSurface is null)
        {
            throw new Exception("Windowing platform doesn't support Vulkan.");
        }
    }

    private void OnLoad()
    {
        Console.WriteLine($"Window Loaded");
        var input = Window.CreateInput();
        foreach (var keyboard in input.Keyboards)
        {
            // TODO: Handle key codes properly, this is a workaround. Try a map of some sort?
            keyboard.KeyDown += (_, key, _) => InputManager.KeyDown(new KeyEventArgs(Enum.Parse<Keycode>(key.ToString())));
            keyboard.KeyUp += (_, key, _) => InputManager.KeyUp(new KeyEventArgs(Enum.Parse<Keycode>(key.ToString())));
        }

        foreach (var mouse in input.Mice)
        {
            // TODO: Handle key codes properly, this is a workaround. Try a map of some sort?
            mouse.MouseDown += (m, button) => InputManager.MouseButtonDown(new MouseEventArgs(
                Enum.Parse<MouseButton>(button.ToString()),
                m.Position.X, m.Position.Y));
            mouse.MouseUp += (m, button) => InputManager.MouseButtonUp(new MouseEventArgs(
                Enum.Parse<MouseButton>(button.ToString()),
                m.Position.X, m.Position.Y));
        }
    }
    private void OnUpdate(double deltaTime) {}
    private void OnRender(double deltaTime) {}

    public void Run()
    {
        Window.Run();
    }

    public void Close()
    {
        Window.Close();
    }
}