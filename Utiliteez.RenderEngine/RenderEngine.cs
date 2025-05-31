using System.Numerics;
using System.Runtime.InteropServices;
using Autofac;
using Castle.Core.Resource;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using Utiliteez.Render;
using Utiliteez.RenderEngine.Interfaces;
using Utiliteez.RenderEngine.Objects;
using Utiliteez.RenderEngine.Structs;

namespace Utiliteez.RenderEngine;

public unsafe record RenderEngine(
    IWindowManager WindowManager,
    IInstanceManager InstanceManager,
    ISurfaceManager SurfaceManager,
    IDeviceManager DeviceManager,
    ISwapChainManager SwapChainManager,
    IResourceManager ResourceManager,
    IPipelineManager PipelineManager,
    ICommandManager CommandManager,
    IAssetManager AssetManager,
    IInputManager InputManager,
    ITimingManager TimingManager,
    ICameraManager CameraManager,
    Vk Vk // Temp
)
{
    public void Initialize()
    {
        WindowManager.CreateWindow();
        InstanceManager.Initialize();
        SurfaceManager.Initialize();
        DeviceManager.Initialize();
        SwapChainManager.Initialize();
        ResourceManager.Initialize();
        PipelineManager.Initialize();
        CommandManager.Initialize();
        InputManager.Initialize();

        InputManager.BindCamera(CameraManager);
        CameraManager.BindInput(InputManager);
        
        
        // ========== TEMPT BEGIN ==========

        
        // Load Scene
        // AssetManager.RegisterModel("ground", "Models/Ground.obj");
        // AssetManager.RegisterModel("powerpole", "Models/PowerPole2.obj");


        // Register Input Events
        InputManager.OnKeyPressed += e => Console.WriteLine($"Key Pressed: {e.Key}, Time Held: {e.TimeHeld}, Time Since Last Pressed: {e.TimeSinceLastPressed}");
        InputManager.OnKeyDown += e =>
        {
            if (e.Key == Keycode.Escape) WindowManager.Close();
        }; 
        
        // Main Loop Here???
        
        // Update Scene
        // var drawOrders = new []
        // {
        //     new DrawOrder
        //     {
        //         model = "ground",
        //         position = [new(0, 0, 0), new(1, 0, 0), new(2, 0, 0)],
        //     },
        //     new DrawOrder
        //     {
        //         model = "powerpole",
        //         position = [new(0, 0, 0),new(1, 0, 0),new(0, 0, 1)],
        //     },
        //     new DrawOrder
        //     {
        //         model = "powerpole",
        //         position = [new(0, 2, 0),new(1, 2, 0),new(0, 2, 1)],
        //     },
        // };
        //
        //
        // // Call RenderFrame instead of binding  
        // WindowManager.Window.Render += (delta) =>
        // {
        //     CommandManager.RenderFrame();
        // };
        //
        // WindowManager.Run();
        // ========== TEMPT END ==========
    }
    
    public bool Close()
    {
        return WindowManager.Close();
    }

    public void RenderScene(DrawOrder[] drawOrders)
    {
        CommandManager.CreateDrawcommand(drawOrders);
        CommandManager.RenderFrame();
    }
    
    public void RegisterModel(string id, string filePath)
    {
        AssetManager.RegisterModel(id, filePath);
    }
    
    public void RegisterFont(Font font)
    {
        AssetManager.RegisterFont(font);
    }
    
    public uint RegisterMaterial(Material material)
    {
        return AssetManager.RegisterMaterial(material);
    }
    
    public void FinalizeAssets()
    {
        AssetManager.Finalize();
    }

    public double GetTime()
    {
        return TimingManager.Now;
    }
}