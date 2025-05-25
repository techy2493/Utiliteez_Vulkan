using Silk.NET.Vulkan;
using Utiliteez.RenderEngine.Structs;

namespace Utiliteez.RenderEngine.Interfaces;

public interface IDeviceManager
{
    void Initialize();
    Vk Vk { get; init; }
    IInstanceManager InstanceManager { get; init; }
    PhysicalDevice PhysicalDevice { get; }
    Device LogicalDevice { get; }
    QueueFamilyIndices Indices { get; }
    ref readonly CommandBuffer CommandBuffer { get; }
    ref readonly Queue GraphicsQueue { get; }
    ref readonly Queue PresentQueue { get; }
    CommandBuffer BeginSingleTimeCommands();
    void EndSingleTimeCommands(CommandBuffer cmd);
}