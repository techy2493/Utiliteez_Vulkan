using Silk.NET.Vulkan;
using Utiliteez.RenderEngine.Interfaces;

namespace Utiliteez.RenderEngine;

public interface IPipelineManager
{
    ref readonly DescriptorSet DescriptorSet { get; }
    Vk Vk { get; init; }
    IDeviceManager DeviceManager { get; init; }
    IResourceManager ResourceManager { get; init; }
    ISwapChainManager SwapChainManager { get; init; }
    void Initialize();
    void WriteDescriptorsForUBO();
    void WriteDescriptorsForMaterialBuffer();

    public  Pipeline GraphicsPipeline { get; }
    public  PipelineLayout GraphicsPipelineLayout  { get; }
    bool Equals(PipelineManager? other);
    bool Equals(object? other);
    int GetHashCode();
    void Deconstruct(out Vk Vk, out IDeviceManager DeviceManager, out IResourceManager ResourceManager, out ISwapChainManager SwapChainManager);
    string ToString();
}