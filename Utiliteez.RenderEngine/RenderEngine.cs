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

        // ========== TEMPT BEGIN ==========

        // Register Assets
        AssetManager.RegisterModel("ground", "Models/Ground.obj");
        AssetManager.RegisterModel("powerpole", "Models/PowerPole2.obj");

        AssetManager.Finalize();
        

        var pole = AssetManager.Models["powerpole"];
        var ground = AssetManager.Models["ground"];

        var drawOrders = new []
        {
            new DrawOrder
            {
                model = ground,
                position = [new(0, 0, 0), new(1, 0, 0), new(2, 0, 0)],
            },
            new DrawOrder
            {
                model = pole,
                position = [new(0, 0, 0),new(1, 0, 0),new(0, 0, 1)],
            },
            new DrawOrder
            {
                model = pole,
                position = [new(0, 2, 0),new(1, 2, 0),new(0, 2, 1)],
            },
        };
        


        // 3) Load Buffers for Drawing
        var drawCommandsList = new List<DrawIndexedIndirectCommand>();
        var instanceCount = 0;
        for (int i = 0; i < drawOrders.Length; i++)
        {
            drawCommandsList.Add(new DrawIndexedIndirectCommand
            {
                IndexCount = drawOrders[i].model.IndexCount,
                InstanceCount = (uint) drawOrders[i].position.Count ,
                FirstIndex = drawOrders[i].model.IndexOffset,
                VertexOffset = drawOrders[i].model.VertexOffset,
                FirstInstance = (uint) instanceCount
            });
            
            instanceCount += drawOrders[i].position.Count;
        }

        var drawCommands = drawCommandsList.ToArray();
        ResourceManager.CreateAndUpdateIndirectDrawCommandBuffer(drawCommands);


        var instanceData = drawOrders.SelectMany(order => order.position, (order, position) =>
        {
            return new InstanceData
            {
                Model = Matrix4x4.CreateTranslation(position),
                MaterialIndex = order.model.MaterialIndex
            };
        }).ToArray();
        ResourceManager.UpdateInstanceDataBuffer(instanceData);

        
        InputManager.OnKeyPressed += e => Console.WriteLine($"Key Pressed: {e.Key}, Time Held: {e.TimeHeld}, Time Since Last Pressed: {e.TimeSinceLastPressed}");
        CommandManager.Initialize();
        Console.WriteLine($"[DEBUG] DrawIndexedIndirectCommand size = {Marshal.SizeOf<DrawIndexedIndirectCommand>()} bytes");
        Console.WriteLine($"[DEBUG] InstanceData size = {Marshal.SizeOf<InstanceData>()} bytes");
        
        
        WindowManager.Window.Render += (delta) =>
        {
            CommandManager.RenderFrame();
        };

        WindowManager.Run();
        // ========== TEMPT END ==========
    }
}