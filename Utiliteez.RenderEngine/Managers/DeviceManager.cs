using System.Runtime.CompilerServices;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Utiliteez.RenderEngine.Interfaces;
using Utiliteez.RenderEngine.Structs;

namespace Utiliteez.RenderEngine;

public unsafe record DeviceManager(
    Vk Vk,
    IInstanceManager InstanceManager,
    ISurfaceManager SurfaceManager
    ) : IDeviceManager
{
    public PhysicalDevice PhysicalDevice { get; private set; }

    private Device _logicalDevice;
    public Device LogicalDevice => _logicalDevice;
    public bool PhysicalDeviceChosen;

    public QueueFamilyIndices Indices { get; private set; }

    public ref readonly Queue GraphicsQueue => ref _graphicsQueue;

    public ref readonly Queue PresentQueue => ref _presentQueue;

    public ref readonly CommandBuffer CommandBuffer => ref _commandBuffer;

    private CommandPool commandPool;
    private CommandBuffer _commandBuffer;
    private Queue _graphicsQueue;
    private Queue _presentQueue;


    public void Initialize()
    {
        PickPhysicalDevice();
        PopulateQueueFamilyIndices();
        CreateLogicalDevice();
        PopulateQueues();
        CreateCommandPool();
        CreateCommandBuffer();
    }

    public void PickPhysicalDevice()
    {
        var devices = Vk!.GetPhysicalDevices(InstanceManager.Instance);

        foreach (var device in devices)
        {
            if (IsDeviceSuitable(device))
            {
                PhysicalDevice = device;
                break;
            }
        }

        if (PhysicalDevice.Handle == 0)
        {
            throw new Exception("failed to find a suitable GPU!");
        }
        
        PhysicalDeviceChosen = true;
    }

    private bool IsDeviceSuitable(PhysicalDevice device)
    {
        var indices = FindQueueFamilies(device);

        bool extensionsSupported = InstanceManager.CheckDeviceExtensionsSupport(device);
        
        bool swapChainAdequate = false;
        if (extensionsSupported)
        {
            var swapChainSupport = SwapChainManager.QuerySwapChainSupport(device, SurfaceManager.SurfaceKhr, SurfaceManager.KhrSurface);
            swapChainAdequate = swapChainSupport.Formats.Any() && swapChainSupport.PresentModes.Any();
        }
        
        
        return indices.IsComplete() && extensionsSupported && swapChainAdequate;
    }

    internal void CreateLogicalDevice()
    {
        if (!PhysicalDeviceChosen)
        {
            throw new Exception("Not Ready! Physical device not chosen.");
        }
        
        PhysicalDeviceFeatures deviceFeatures = new() {};
        PhysicalDeviceDynamicRenderingFeaturesKHR dynamicRenderingFeatures = new()
        {
            SType = StructureType.PhysicalDeviceDynamicRenderingFeaturesKhr,
            DynamicRendering = true
        };
        var (mem, queueCreateInfos) = CreateDeviceQueueCreateInfos();

        DeviceCreateInfo createInfo = new()
        {
            SType = StructureType.DeviceCreateInfo,
            PNext = &dynamicRenderingFeatures,
            PQueueCreateInfos = (DeviceQueueCreateInfo*)queueCreateInfos,
            PEnabledFeatures = &deviceFeatures,
            QueueCreateInfoCount = (uint)Indices.ToDistinctArray().Length,
            
            EnabledExtensionCount = (uint)InstanceManager.DeviceExtensions.Length,
            PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(InstanceManager.DeviceExtensions)
        };


        if (InstanceManager.EnableValidationLayers)
        {
            createInfo.EnabledLayerCount = (uint)InstanceManager.ValidationLayers.Length;
            createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(InstanceManager.ValidationLayers);
        }
        else
        {
            createInfo.EnabledLayerCount = 0;
        }

        if (Vk!.CreateDevice(PhysicalDevice, in createInfo, null, out _logicalDevice) != Result.Success)
        {
            throw new Exception("failed to create logical device!");
        }

        mem.Dispose();
        

        if (InstanceManager.EnableValidationLayers)
        {
            SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
        }
        
        SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);
    }
    
    
    public void PopulateQueueFamilyIndices()
    {
        Indices = FindQueueFamilies();
    }
    
    public  (GlobalMemory mem, IntPtr queues) CreateDeviceQueueCreateInfos()
    {
        
        var uniqueQueueFamilies = Indices.ToDistinctArray();

        var mem = GlobalMemory.Allocate(uniqueQueueFamilies.Length * sizeof(DeviceQueueCreateInfo));
        var queueCreateInfos = (DeviceQueueCreateInfo*)Unsafe.AsPointer(ref mem.GetPinnableReference());

        float queuePriority = 1.0f;
        for (int i = 0; i < uniqueQueueFamilies.Length; i++)
        {
            queueCreateInfos[i] = new()
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = uniqueQueueFamilies[i],
                QueueCount = 1,
                PQueuePriorities = &queuePriority
            };
        }

        return (mem, (IntPtr)queueCreateInfos);
    }
    
    public void PopulateQueues()
    {
        Vk!.GetDeviceQueue(LogicalDevice, Indices.GraphicsFamily!.Value, 0, out _graphicsQueue);
        Vk!.GetDeviceQueue(LogicalDevice, Indices.PresentFamily!.Value, 0, out _presentQueue);
        
        if (_graphicsQueue.Handle == 0 || _presentQueue.Handle == 0)
            throw new InvalidOperationException("Failed to grab required queues!");
    }
    
    public QueueFamilyIndices FindQueueFamilies(PhysicalDevice physicalDevice = default)
    {
        if (physicalDevice.Handle == 0)
        {
            physicalDevice = PhysicalDevice;
        }
        var indices = new QueueFamilyIndices();

        uint queueFamilityCount = 0;
        Vk!.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, ref queueFamilityCount, null);

        var queueFamilies = new QueueFamilyProperties[queueFamilityCount];
        fixed (QueueFamilyProperties* queueFamiliesPtr = queueFamilies)
        {
            Vk!.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, ref queueFamilityCount, queueFamiliesPtr);
        }


        uint i = 0;
        foreach (var queueFamily in queueFamilies)
        {
            if (queueFamily.QueueFlags.HasFlag(QueueFlags.GraphicsBit))
            {
                indices.GraphicsFamily = i;
            }

            SurfaceManager.KhrSurface.GetPhysicalDeviceSurfaceSupport(physicalDevice, i, SurfaceManager.SurfaceKhr, out var presentSupport);

            if (presentSupport)
            {
                indices.PresentFamily = i;
            }

            if (indices.IsComplete())
            {
                break;
            }

            i++;
        }

        return indices;
    }
    

    internal void CreateCommandPool()
    {
        var queueFamiliyIndicies = FindQueueFamilies();

        CommandPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = queueFamiliyIndicies.GraphicsFamily!.Value,
            Flags = CommandPoolCreateFlags.ResetCommandBufferBit, // Add this flag
        };

        if (Vk!.CreateCommandPool(LogicalDevice, in poolInfo, null, out commandPool) != Result.Success) {
            throw new Exception("failed to create command pool!");
        }
    }
    
    public unsafe CommandBuffer BeginSingleTimeCommands()
    {
        // Allocate a one-time command buffer from the existing pool
        var allocInfo = new CommandBufferAllocateInfo
        {
            SType              = StructureType.CommandBufferAllocateInfo,
            CommandPool        = commandPool,
            Level              = CommandBufferLevel.Primary,
            CommandBufferCount = 1
        };
        Vk.AllocateCommandBuffers(LogicalDevice, &allocInfo, out var cmd);

        // Begin recording with the OneTimeSubmit flag
        var beginInfo = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };
        Vk.BeginCommandBuffer(cmd, &beginInfo);

        return cmd;
    }

    public unsafe void EndSingleTimeCommands(CommandBuffer cmd)
    {
        // Finish recording
        Vk.EndCommandBuffer(cmd);

        // Submit it on the graphics queue (also handles transfers)
        var submit = new SubmitInfo
        {
            SType              = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers    = &cmd
        };
        Vk.QueueSubmit(_graphicsQueue, 1, &submit, default);
        Vk.QueueWaitIdle(_graphicsQueue);

        // Free the temporary command buffer
        Vk.FreeCommandBuffers(LogicalDevice, commandPool, 1, &cmd);
    }
    
    internal void CreateCommandBuffer()
    {
        // Allocate a single command buffer
        _commandBuffer = default; // Assuming a single CommandBuffer field
        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1, // Only one command buffer
        };

        fixed (CommandBuffer* commandBufferPtr = &_commandBuffer)
        {
            if (Vk!.AllocateCommandBuffers(LogicalDevice, in allocInfo, commandBufferPtr) != Result.Success)
            {
                throw new Exception("failed to allocate command buffer!");
            }
        }
    }
}