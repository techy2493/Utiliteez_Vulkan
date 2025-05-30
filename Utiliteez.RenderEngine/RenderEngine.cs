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


        // drawRepo.AddModel(RR.Request("Models/coaster-mouse-curve.obj"), Vector3.Zero);
        // drawRepo.AddModel(RR.Request("Models/Ground.obj"), new Vector3(0,0,0));
        // drawRepo.AddModel(RR.Request("Models/Ground.obj"), new Vector3(1,0,0));
        // drawRepo.AddModel(RR.Request("Models/PowerPole2.obj"), new Vector3(0,0,0));
        // drawRepo.AddModel(RR.Request("Models/PowerPole2.obj"), new Vector3(1,0,0));
        // // drawRepo.AddModel(RR.Request("Models/door-rotate-square-c.obj"), new Vector3(5,0,0));
        // drawRepo.AddModel(RR.Request("Models/door-rotate-square-c.obj"), new Vector3(6,0,0));

        // Register Assets
        AssetManager.RegisterModel("ground", "Models/Ground.obj");
        AssetManager.RegisterModel("powerpole", "Models/PowerPole2.obj");

        // Generate Texture Atlas
        ResourceManager.UpdateTextureAtlas();

        // Create Buffers
        ResourceManager.CreateMaterialBuffer();
        PipelineManager.WriteDescriptorsForMaterialBuffer();
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
        


        // 1) Create Vertex and Index Buffers
        #region Vertex and Index Buffers
        var vert = AssetManager.Vertices.ToArray();
        var Indices = AssetManager.Indices.ToArray();

        var vertexBufferSize = (ulong)(vert.Length * Marshal.SizeOf<Vertex>());
        var indexBufferSize = (ulong)(Indices.Length * sizeof(uint));

        VulkanBuffer vertexBuffer = VulkanBuffer.CreateBuffer(Vk, DeviceManager.LogicalDevice,
            DeviceManager.PhysicalDevice, vertexBufferSize, BufferUsageFlags.VertexBufferBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
        VulkanBuffer indexBuffer = VulkanBuffer.CreateBuffer(Vk, DeviceManager.LogicalDevice,
            DeviceManager.PhysicalDevice, indexBufferSize, BufferUsageFlags.IndexBufferBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

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
#endregion


        // 3) Load Buffers for Drawing

        #region DrawCommands &  Buffer



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

        var indirectBufferSize = (ulong)(drawCommands.Length * Marshal.SizeOf<DrawIndexedIndirectCommand>());
        var indirectBuffer = VulkanBuffer.CreateBuffer(Vk, DeviceManager.LogicalDevice, DeviceManager.PhysicalDevice,
            indirectBufferSize,
            BufferUsageFlags.IndirectBufferBit | BufferUsageFlags.TransferDstBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

        

        void* mapped;
        Vk.MapMemory(DeviceManager.LogicalDevice,
            indirectBuffer.Memory,
            0,
            indirectBufferSize,
            0,
            &mapped);

        IntPtr basePtr = (IntPtr)mapped;
        int structSize = Marshal.SizeOf<DrawIndexedIndirectCommand>();
        for (int i = 0; i < drawCommands.Length; i++)
        {
            // copy each struct into the mapped region at the correct byte offset
            Marshal.StructureToPtr(
                drawCommands[i],
                basePtr + i * structSize,
                false
            );
        }

        Vk.UnmapMemory(DeviceManager.LogicalDevice, indirectBuffer.Memory);

        #endregion

        #region InstanceData & Buffer

        var instanceData = drawOrders.SelectMany(order => order.position, (order, position) =>
        {
            return new InstanceData
            {
                Model = Matrix4x4.CreateTranslation(position),
                MaterialIndex = order.model.MaterialIndex
            };
        }).ToArray();
        ResourceManager.UpdateInstanceDataBuffer(instanceData);
        
        #endregion

        CommandManager.Initialize();
        Console.WriteLine($"[DEBUG] DrawIndexedIndirectCommand size = {Marshal.SizeOf<DrawIndexedIndirectCommand>()} bytes");
        Console.WriteLine($"[DEBUG] InstanceData size = {Marshal.SizeOf<InstanceData>()} bytes");
        WindowManager.Window.Render += (delta) =>
        {
            CommandManager.RenderFrame(vertexBuffer, indexBuffer, indirectBuffer, ResourceManager.InstanceDataBuffer, (uint) drawCommands.Length);
        };

        WindowManager.Run();
        // ========== TEMPT END ==========
    }
}