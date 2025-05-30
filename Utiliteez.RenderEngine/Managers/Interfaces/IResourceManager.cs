using Silk.NET.Vulkan;
using Utiliteez.RenderEngine.Interfaces;
using Utiliteez.RenderEngine.Structs;

namespace Utiliteez.RenderEngine;

public interface IResourceManager
{
    VulkanBuffer UniformBuffer { get; }
    VulkanBuffer MaterialBuffer { get; }
    VulkanBuffer IndexBuffer { get; }
    VulkanBuffer VertexBuffer { get; }
    VulkanBuffer IndirectDrawCommandsBuffer { get;}
    
    ulong MaterialBufferSize { get; }
    
    ulong IndirectDrawCommandsBufferSize { get; }
    public uint IndirectDrawCommandCount { get; }
    VulkanBuffer InstanceDataBuffer { get; }
    ulong InstanceDataBufferSize { get; }
    Image AtlasImage { get; }
    DeviceMemory AtlasImageMemory { get; }
    ImageView AtlasImageView { get; }
    Sampler AtlasSampler { get; }
    uint AtlasMipLevels { get; }
    Vk Vk { get; init; }
    IDeviceManager DeviceManager { get; init; }
    ISwapChainManager SwapChainManager { get; init; }
    void SetAtlasDescriptorSet(DescriptorSet ds);
    unsafe void CreateMaterialBuffer(ShaderMaterial[] materials);
    unsafe void CreateInstanceDataBuffer();
    unsafe void CreateIndexAndVertexDataBuffers(uint[] indices, Vertex[] vertices);
    unsafe void UpdateInstanceDataBuffer(InstanceData[] instanceData);
    void Initialize();
    unsafe void UpdateCameraUniformBuffer();
    unsafe void CreateTextureAtlas();
    unsafe void UpdateTextureAtlas();
    void CreateAndUpdateIndirectDrawCommandBuffer(DrawIndexedIndirectCommand[] drawCommands);

    /// <summary>
    /// Create a VkImage + allocate & bind its memory.
    /// </summary>
    unsafe void CreateImage(
        uint width,
        uint height,
        Format format,
        ImageTiling tiling,
        ImageUsageFlags usage,
        MemoryPropertyFlags properties,
        out Image image,
        out DeviceMemory imageMemory);

    /// <summary>
    /// Create an ImageView for the given VkImage.
    /// </summary>
    unsafe ImageView CreateImageView(
        Image image,
        Format format,
        ImageAspectFlags aspectFlags,
        uint mipLevels);

    /// <summary>
    /// Insert a pipeline barrier to transition an image between layouts.
    /// Must be called while 'cmd' is recording.
    /// </summary>
    unsafe void TransitionImageLayout(
        CommandBuffer cmd,
        Image image,
        Format format,
        ImageLayout oldLayout,
        ImageLayout newLayout);

    /// <summary>
    /// Record a copy from a linear buffer into an optimal‐tiling image.
    /// Must be called while 'cmd' is recording, and image must be in TransferDstOptimal.
    /// </summary>
    unsafe void CopyBufferToImage(
        CommandBuffer cmd,
        Silk.NET.Vulkan.Buffer buffer,
        Image image,
        uint width,
        uint height);

    bool Equals(ResourceManager? other);
    bool Equals(object? other);
    int GetHashCode();
    void Deconstruct(out Vk Vk, out IDeviceManager DeviceManager, out ISwapChainManager SwapChainManager);
    string ToString();
}