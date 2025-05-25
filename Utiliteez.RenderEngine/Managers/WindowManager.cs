using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Utiliteez.RenderEngine;
// Todo: Move this shit?? IWindow in common? Window in Game? Or just expose an event sink?
public class WindowManager : IWindowManager
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

        Window.Initialize();
        
        if (Window.VkSurface is null)
        {
            throw new Exception("Windowing platform doesn't support Vulkan.");
        }
        Window.Load += OnLoad;
        Window.Update += OnUpdate;
        Window.Render += OnRender;
    }

    private void OnLoad() 
    {
        var input = Window.CreateInput();
        for (int i = 0; i < input.Keyboards.Count; i++)
            input.Keyboards[i].KeyDown += KeyDown;
    }
    private void OnUpdate(double deltaTime) {}
    private void OnRender(double deltaTime) {}

    private void KeyDown(IKeyboard keyboard, Key key, int Keycode)
    {
        if (key == Key.Escape)
        {
            Window.Close();
        }
    }

    public void Run()
    {
        Window.Run();
    }
}