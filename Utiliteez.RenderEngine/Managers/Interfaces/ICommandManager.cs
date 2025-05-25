using Silk.NET.Vulkan;
using Utiliteez.RenderEngine.Interfaces;

namespace Utiliteez.RenderEngine;

public interface ICommandManager
{
    unsafe void RenderFrame(
        VulkanBuffer vertexBuffer,
        VulkanBuffer indexBuffer,
        VulkanBuffer IndirectCommandBuffer,
        VulkanBuffer InstanceDataBuffer,
        uint drawCount);

    Vk Vk { get; init; }
    IDeviceManager DeviceManager { get; init; }
    ISwapChainManager SwapChainManager { get; init; }
    IResourceManager ResourceManager { get; init; }
    IPipelineManager PipelineManager { get; init; }
    bool Equals(CommandManager? other);
    bool Equals(object? other);
    int GetHashCode();
    void Deconstruct(out Vk Vk, out IDeviceManager DeviceManager, out ISwapChainManager SwapChainManager, out IResourceManager ResourceManager, out IPipelineManager PipelineManager);
    string ToString();
    void Initialize();
}