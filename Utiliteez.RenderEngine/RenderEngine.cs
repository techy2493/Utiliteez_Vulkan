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
        
        
        ResourceRepository<Model>  RR = new ResourceRepository<Model>(new ModelLoader());
        
        var drawRepo = new DrawRepo();
        
        // drawRepo.AddModel(RR.Request("Models/coaster-mouse-curve.obj"), Vector3.Zero);
        drawRepo.AddModel(RR.Request("Models/Ground.obj"), new Vector3(0,0,0));
        drawRepo.AddModel(RR.Request("Models/Ground.obj"), new Vector3(1,0,0));
        drawRepo.AddModel(RR.Request("Models/PowerPole2.obj"), new Vector3(0,0,0));
        drawRepo.AddModel(RR.Request("Models/PowerPole2.obj"), new Vector3(1,0,0));
        // drawRepo.AddModel(RR.Request("Models/door-rotate-square-c.obj"), new Vector3(5,0,0));

        var mats = drawRepo.Materials;
        var vert = drawRepo.Vertices;
        var Indices = drawRepo.Indices;
        
        TextureAtlas.Generate(mats);
        ResourceManager.UpdateTextureAtlas();

        var shaderMats = mats.Select(x => (ShaderMaterial)x).ToArray();
        ResourceManager.CreateMaterialBuffer(shaderMats);
        PipelineManager.WriteDescriptorsForMaterialBuffer();
        
        var vertexBufferSize = (ulong)(vert.Length * Marshal.SizeOf<Vertex>());
        var indexBufferSize = (ulong)(Indices.Length * sizeof(uint));

        VulkanBuffer vertexBuffer = VulkanBuffer.CreateBuffer(Vk, DeviceManager.LogicalDevice, DeviceManager.PhysicalDevice, vertexBufferSize, BufferUsageFlags.VertexBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
        VulkanBuffer indexBuffer = VulkanBuffer.CreateBuffer(Vk, DeviceManager.LogicalDevice, DeviceManager.PhysicalDevice, indexBufferSize, BufferUsageFlags.IndexBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

        void* vertexData;
        Vk.MapMemory(DeviceManager.LogicalDevice, vertexBuffer.Memory, 0, vertexBufferSize, 0, &vertexData);

        IntPtr vertexDataPtr = (IntPtr)vertexData;
        for (int i = 0; i < vert.Length; i++)
        {
            Marshal.StructureToPtr(vert[i], vertexDataPtr + i * Marshal.SizeOf<Vertex>(), false);
        }
        Vk.UnmapMemory(DeviceManager.LogicalDevice, vertexBuffer.Memory);

        int[] intIndices = Array.ConvertAll(Indices, x => unchecked((int)x));

        void* indexData;
        Vk.MapMemory(DeviceManager.LogicalDevice, indexBuffer.Memory, 0, indexBufferSize, 0, &indexData);
        Marshal.Copy(intIndices, 0, (IntPtr)indexData, Indices.Length);
        Vk.UnmapMemory(DeviceManager.LogicalDevice, indexBuffer.Memory);
        
        WindowManager.Window.Render += (delta) =>
        {
            CommandManager.RenderFrame(vertexBuffer, indexBuffer, (uint)Indices.Length);
        };
        
        WindowManager.Run();
        // ========== TEMPT END ==========
    }
    
    
    
}