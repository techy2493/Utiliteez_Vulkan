using Silk.NET.Vulkan;
using Utiliteez.RenderEngine.Interfaces;
using Utiliteez.RenderEngine.Structs;

namespace Utiliteez.RenderEngine;

public interface IResourceManager
{
    void Initialize();
    void UpdateUniformBuffer();
    void CreateMaterialBuffer();
    void UpdateInstanceDataBuffer(InstanceData[] instanceData);
    void CreateInstanceDataBuffer();
    void UpdateTextureAtlas();
    void SetAtlasDescriptorSet(DescriptorSet ds);
    VulkanBuffer UniformBuffer { get; }
    VulkanBuffer MaterialBuffer { get; }
    ulong MaterialBufferSize { get; }
    VulkanBuffer InstanceDataBuffer { get; }
    ulong InstanceDataBufferSize { get; }
    Image        AtlasImage { get; }
    DeviceMemory AtlasImageMemory { get; }
    ImageView    AtlasImageView { get; }
    Sampler      AtlasSampler { get; }
    uint         AtlasMipLevels { get; }
    Vk Vk { get; init; }
    IDeviceManager DeviceManager { get; init; }
}