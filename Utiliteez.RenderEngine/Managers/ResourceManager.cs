using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Utiliteez.Render;
using Utiliteez.RenderEngine.Interfaces;
using Utiliteez.RenderEngine.Objects;
using Utiliteez.RenderEngine.Structs;
using Buffer = System.Buffer;

namespace Utiliteez.RenderEngine;

public unsafe record ResourceManager(
    Vk Vk,
    IDeviceManager DeviceManager,
    ISwapChainManager SwapChainManager,
    IAssetManager AssetManager
) : IResourceManager
{
    public VulkanBuffer UniformBuffer { get; private set; }
    public VulkanBuffer MaterialBuffer { get; private set; }
    public ulong MaterialBufferSize { get; private set; }
    public VulkanBuffer InstanceDataBuffer { get; private set; }
    public ulong InstanceDataBufferSize { get; private set; }
    const int MaxInstances = 2; // max instances per model

    // New Vulkan handles for the atlas
    private Image _atlasImage;
    public Image AtlasImage => _atlasImage;
    private DeviceMemory _atlasImageMemory;
    public DeviceMemory AtlasImageMemory => _atlasImageMemory;
    public ImageView AtlasImageView { get; private set; }
    private Sampler _atlasSampler;
    public Sampler AtlasSampler => _atlasSampler;
    public uint AtlasMipLevels { get; private set; }
    private DescriptorSet _atlasDescriptorSet;
    public void SetAtlasDescriptorSet(DescriptorSet ds) => _atlasDescriptorSet = ds;

    internal void CreateUniformBuffer()
    {
        ulong bufferSize = (ulong)sizeof(CameraUniformBufferObject);

        UniformBuffer = VulkanBuffer.CreateBuffer(Vk, DeviceManager.LogicalDevice, DeviceManager.PhysicalDevice,
            bufferSize, BufferUsageFlags.UniformBufferBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
    }

    public void CreateMaterialBuffer()
    {
        var materials = AssetManager.Materials.Select(x => (ShaderMaterial)x).ToArray();
        
        ulong entrySize = (ulong)Marshal.SizeOf<ShaderMaterial>();
        MaterialBufferSize = entrySize * (ulong)Math.Max(materials.Length, 1);

        MaterialBuffer = VulkanBuffer.CreateBuffer(Vk, DeviceManager.LogicalDevice, DeviceManager.PhysicalDevice,
            MaterialBufferSize, BufferUsageFlags.StorageBufferBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

        // 3) Map it
        void* gpuPtr;
        Vk.MapMemory(
            DeviceManager.LogicalDevice,
            MaterialBuffer.Memory,
            0,
            MaterialBufferSize,
            MemoryMapFlags.None,
            &gpuPtr
        );

        if (materials.Length > 0)
        {
            // pin the managed array so GC won’t move it
            fixed (ShaderMaterial* srcPtr = &materials[0])
            {
                Buffer.MemoryCopy(
                    srcPtr,
                    gpuPtr,
                    MaterialBufferSize, // destination capacity
                    (ulong)materials.Length * entrySize // bytes to copy
                );
            }
        }
        else
        {
            // optionally zero the single slot
            Unsafe.InitBlock(gpuPtr, 0, (uint)entrySize);
        }

        // 5) Unmap
        Vk.UnmapMemory(DeviceManager.LogicalDevice, MaterialBuffer.Memory);
    }

    public void CreateInstanceDataBuffer()
    {
        InstanceDataBufferSize = (ulong)(MaxInstances * Marshal.SizeOf<InstanceData>());
        InstanceDataBuffer = VulkanBuffer.CreateBuffer(Vk, DeviceManager.LogicalDevice, DeviceManager.PhysicalDevice,
            InstanceDataBufferSize,
            BufferUsageFlags.StorageBufferBit | BufferUsageFlags.TransferDstBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

        InstanceData[] instanceData = [];

// 4) Map → copy → unmap
        void* mappedInst;
        Vk.MapMemory(
            DeviceManager.LogicalDevice,
            InstanceDataBuffer.Memory,
            0,
            InstanceDataBufferSize,
            0,
            &mappedInst
        );

        IntPtr baseInstPtr = (IntPtr)mappedInst;
        int instStructSize = Marshal.SizeOf<InstanceData>();
        for (int i = 0; i < instanceData.Length; i++)
        {
            Marshal.StructureToPtr(
                instanceData[i],
                baseInstPtr + i * instStructSize,
                false
            );
        }

        Vk.UnmapMemory(DeviceManager.LogicalDevice, InstanceDataBuffer.Memory);
    }

    public void UpdateInstanceDataBuffer(InstanceData[] instanceData)
    {
        void* mappedInst;
        Vk.MapMemory(
            DeviceManager.LogicalDevice,
            InstanceDataBuffer.Memory,
            0,
            InstanceDataBufferSize,
            0,
            &mappedInst
        );

        IntPtr baseInstPtr = (IntPtr)mappedInst;
        int instStructSize = Marshal.SizeOf<InstanceData>();
        for (int i = 0; i < instanceData.Length; i++)
        {
            Marshal.StructureToPtr(
                instanceData[i],
                baseInstPtr + i * instStructSize,
                false
            );
        }

        Vk.UnmapMemory(DeviceManager.LogicalDevice, InstanceDataBuffer.Memory);
    }

    public void Initialize()
    {
        CreateUniformBuffer();
        CreateMaterialBuffer();
        CreateInstanceDataBuffer();
        CreateTextureAtlas();
    }

    public void UpdateUniformBuffer()
    {
        CameraUniformBufferObject ubo = new CameraUniformBufferObject();

        // 1) Set up an orthographic projection
        float viewWidth = 3.0f; // world-space width you want visible
        float viewHeight = viewWidth *
                           (SwapChainManager.SwapChainExtent.Height / (float)SwapChainManager.SwapChainExtent.Width);
        float nearZ = -50.0f;
        float farZ = 50.0f;
        Matrix4x4 proj = Matrix4x4.CreateOrthographicOffCenter(
            -viewWidth * 0.5f, +viewWidth * 0.5f,
            -viewHeight * 0.5f, +viewHeight * 0.5f,
            nearZ, farZ
        );

        // --- isometric angles --- //
// 45° around Y (π/4) + 180° = 225° (5π/4)
        float yaw = MathF.PI * 5f / 4f;
// elevation = arctan(1/√2) ≈ 35.264°
        float elevation = MathF.Atan(1f / MathF.Sqrt(2f));
        float distance = 0.1f;

// build a unit-direction from spherical coords
        Vector3 dir = new Vector3(
            MathF.Cos(elevation) * MathF.Cos(yaw),
            MathF.Sin(elevation),
            MathF.Cos(elevation) * MathF.Sin(yaw)
        );

// instead of target - dir * d, *add* to go to the opposite side:
        Vector3 target = new Vector3(0, 1.5f, 0); // adjust to pole’s center
        Vector3 eye = target + dir * distance;
        Vector3 up = new Vector3(0, -1, 0);

// now your look-at:
        var view = Matrix4x4.CreateLookAt(eye, target, up);

        // 4) Upload your new matrices into your UBO
        ubo.view = view;
        ubo.proj = proj;

        void* data;
        Vk.MapMemory(DeviceManager.LogicalDevice, UniformBuffer.Memory, 0, (ulong)sizeof(CameraUniformBufferObject), 0,
            &data);
        Marshal.StructureToPtr(ubo, (IntPtr)data, false);
        Vk.UnmapMemory(DeviceManager.LogicalDevice, UniformBuffer.Memory);
    }


    public unsafe void CreateTextureAtlas()
    {
        // 1) Pull pixel data from your in-memory atlas
        //    (TextureAtlas.Atlas is your ImageSharp Image<Rgba32>)
        int texW = TextureAtlas.Atlas.Width;
        int texH = TextureAtlas.Atlas.Height;
        byte[] pixels = new byte[texW * texH * 4];
        TextureAtlas.Atlas.CopyPixelDataTo(pixels);
        ulong imageSize = (ulong)pixels.LongLength;

        // 2) Create & fill a staging buffer
        var staging = VulkanBuffer.CreateBuffer(
            Vk,
            DeviceManager.LogicalDevice,
            DeviceManager.PhysicalDevice,
            imageSize,
            BufferUsageFlags.TransferSrcBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit
        );

        void* dataPtr;
        Vk.MapMemory(DeviceManager.LogicalDevice, staging.Memory, 0, imageSize, 0, &dataPtr);
        Marshal.Copy(pixels, 0, (IntPtr)dataPtr, pixels.Length);
        Vk.UnmapMemory(DeviceManager.LogicalDevice, staging.Memory);

        // 3) Create the GPU‐local VkImage
        CreateImage(
            (uint)texW, (uint)texH,
            Format.R8G8B8A8Unorm,
            ImageTiling.Optimal,
            ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit,
            MemoryPropertyFlags.DeviceLocalBit,
            out _atlasImage,
            out _atlasImageMemory
        );

        // 4) Record all transitions + copy in one throw-away command buffer
        var cmd = DeviceManager.BeginSingleTimeCommands();

        TransitionImageLayout(
            cmd, _atlasImage,
            Format.R8G8B8A8Unorm,
            ImageLayout.Undefined,
            ImageLayout.TransferDstOptimal
        );

        CopyBufferToImage(
            cmd,
            staging.Buffer,
            _atlasImage,
            (uint)texW,
            (uint)texH
        );

        TransitionImageLayout(
            cmd, _atlasImage,
            Format.R8G8B8A8Unorm,
            ImageLayout.TransferDstOptimal,
            ImageLayout.ShaderReadOnlyOptimal
        );

        DeviceManager.EndSingleTimeCommands(cmd);

        // 5) Cleanup staging
        Vk.DestroyBuffer(DeviceManager.LogicalDevice, staging.Buffer, null);
        Vk.FreeMemory(DeviceManager.LogicalDevice, staging.Memory, null);

        // 6) Create the ImageView & Sampler
        AtlasImageView = CreateImageView(
            _atlasImage,
            Format.R8G8B8A8Unorm,
            ImageAspectFlags.ColorBit,
            /*mipLevels*/ 1
        );

        var sci = new SamplerCreateInfo
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = Filter.Linear,
            MinFilter = Filter.Linear,
            AddressModeU = SamplerAddressMode.ClampToEdge,
            AddressModeV = SamplerAddressMode.ClampToEdge,
            AddressModeW = SamplerAddressMode.ClampToEdge,
            MipmapMode = SamplerMipmapMode.Linear,
            MinLod = 0,
            MaxLod = 0,
            UnnormalizedCoordinates = false,
            AnisotropyEnable = false,
            BorderColor = BorderColor.IntOpaqueBlack,
            CompareEnable = false,
            
        };
        Vk.CreateSampler(DeviceManager.LogicalDevice, &sci, null, out _atlasSampler);
    }

    public unsafe void UpdateTextureAtlas()
    {
        // 0) Update Atlas
        TextureAtlas.Generate(AssetManager.Materials.ToArray());
        
        // 1) Pull the CPU‐side atlas pixels
        var cpuAtlas = TextureAtlas.Atlas;
        int atlasW = cpuAtlas.Width;
        int atlasH = cpuAtlas.Height;
        ulong imageSize = (ulong)atlasW * (ulong)atlasH * 4;

        // 2) Wait for GPU idle so we can safely destroy the old resources
        Vk.DeviceWaitIdle(DeviceManager.LogicalDevice);

        // 3) Destroy the old atlas resources
        if (AtlasSampler.Handle != 0)
        {
            Vk.DestroySampler(DeviceManager.LogicalDevice, AtlasSampler, null);
        }

        if (AtlasImageView.Handle != 0)
        {
            Vk.DestroyImageView(DeviceManager.LogicalDevice, AtlasImageView, null);
        }

        if (_atlasImage.Handle != 0)
        {
            Vk.DestroyImage(DeviceManager.LogicalDevice, _atlasImage, null);
            Vk.FreeMemory(DeviceManager.LogicalDevice, _atlasImageMemory, null);
        }

        // 4) Create a staging buffer and copy CPU pixels into it
        var staging = VulkanBuffer.CreateBuffer(
            Vk,
            DeviceManager.LogicalDevice,
            DeviceManager.PhysicalDevice,
            imageSize,
            BufferUsageFlags.TransferSrcBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit
        );
        void* dataPtr;
        Vk.MapMemory(DeviceManager.LogicalDevice, staging.Memory, 0, imageSize, 0, &dataPtr);
        cpuAtlas.CopyPixelDataTo(new Span<byte>(dataPtr, (int)imageSize));
        Vk.UnmapMemory(DeviceManager.LogicalDevice, staging.Memory);

        // 5) Create a new optimal‐tiled GPU image
        CreateImage(
            (uint)atlasW, (uint)atlasH,
            Format.R8G8B8A8Unorm,
            ImageTiling.Optimal,
            ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit,
            MemoryPropertyFlags.DeviceLocalBit,
            out _atlasImage,
            out _atlasImageMemory
        );

        // 6) Copy buffer → image with layout transitions in a single‐use command buffer
        var cmd = DeviceManager.BeginSingleTimeCommands();

        // transition to TransferDst
        TransitionImageLayout(
            cmd, _atlasImage, Format.R8G8B8A8Unorm,
            ImageLayout.Undefined,
            ImageLayout.TransferDstOptimal
        );

        // copy
        CopyBufferToImage(cmd, staging.Buffer, _atlasImage, (uint)atlasW, (uint)atlasH);

        // transition to ShaderRead
        TransitionImageLayout(
            cmd, _atlasImage, Format.R8G8B8A8Unorm,
            ImageLayout.TransferDstOptimal,
            ImageLayout.ShaderReadOnlyOptimal
        );

        DeviceManager.EndSingleTimeCommands(cmd);

        // destroy staging
        Vk.DestroyBuffer(DeviceManager.LogicalDevice, staging.Buffer, null);
        Vk.FreeMemory(DeviceManager.LogicalDevice, staging.Memory, null);

        // 7) Recreate view + sampler
        AtlasImageView = CreateImageView(
            _atlasImage,
            Format.R8G8B8A8Unorm,
            ImageAspectFlags.ColorBit,
            /*mipLevels*/ 1
        );
        var sci = new SamplerCreateInfo
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = Filter.Linear,
            MinFilter = Filter.Linear,
            AddressModeU = SamplerAddressMode.ClampToEdge,
            AddressModeV = SamplerAddressMode.ClampToEdge,
            AddressModeW = SamplerAddressMode.ClampToEdge,
            MipmapMode = SamplerMipmapMode.Linear,
            MinLod = 0,
            MaxLod = 0,
            UnnormalizedCoordinates = false,
            AnisotropyEnable = false,
            BorderColor = BorderColor.IntOpaqueBlack,
            CompareEnable = false
        };
        Vk.CreateSampler(DeviceManager.LogicalDevice, &sci, null, out _atlasSampler);

        // 8) Finally rewrite descriptor binding 2 in the existing set
        var imageInfo = new DescriptorImageInfo
        {
            Sampler = AtlasSampler,
            ImageView = AtlasImageView,
            ImageLayout = ImageLayout.ShaderReadOnlyOptimal
        };
        var write = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = _atlasDescriptorSet,
            DstBinding = 2, // atlas binding
            DstArrayElement = 0,
            DescriptorType = DescriptorType.CombinedImageSampler,
            DescriptorCount = 1,
            PImageInfo = &imageInfo
        };
        Vk.UpdateDescriptorSets(
            DeviceManager.LogicalDevice,
            1, &write,
            0, null
        );
    }


    /// <summary>
    /// Create a VkImage + allocate & bind its memory.
    /// </summary>
    public void CreateImage(
        uint width,
        uint height,
        Format format,
        ImageTiling tiling,
        ImageUsageFlags usage,
        MemoryPropertyFlags properties,
        out Image image,
        out DeviceMemory imageMemory)
    {
        // 1) Create the image handle
        var imageInfo = new ImageCreateInfo
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Extent = new Extent3D { Width = width, Height = height, Depth = 1 },
            MipLevels = 1,
            ArrayLayers = 1,
            Format = format,
            Tiling = tiling,
            InitialLayout = ImageLayout.Undefined,
            Usage = usage,
            SharingMode = SharingMode.Exclusive,
            Samples = SampleCountFlags.Count1Bit,
        };
        if (Vk.CreateImage(DeviceManager.LogicalDevice, &imageInfo, null, out image) != Result.Success)
            throw new Exception("Failed to create image.");

        // 2) Allocate memory
        Vk.GetImageMemoryRequirements(DeviceManager.LogicalDevice, image, out MemoryRequirements memReq);
        var allocInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memReq.Size,
            MemoryTypeIndex =
                VulkanBuffer.FindMemoryType(Vk, DeviceManager.PhysicalDevice, memReq.MemoryTypeBits, properties)
        };
        if (Vk.AllocateMemory(DeviceManager.LogicalDevice, &allocInfo, null, out imageMemory) != Result.Success)
            throw new Exception("Failed to allocate image memory.");

        // 3) Bind
        Vk.BindImageMemory(DeviceManager.LogicalDevice, image, imageMemory, 0);
    }

    /// <summary>
    /// Create an ImageView for the given VkImage.
    /// </summary>
    public ImageView CreateImageView(
        Image image,
        Format format,
        ImageAspectFlags aspectFlags,
        uint mipLevels)
    {
        var viewInfo = new ImageViewCreateInfo
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = image,
            ViewType = ImageViewType.ImageViewType2D,
            Format = format,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = aspectFlags,
                BaseMipLevel = 0,
                LevelCount = mipLevels,
                BaseArrayLayer = 0,
                LayerCount = 1
            }
        };

        if (Vk.CreateImageView(DeviceManager.LogicalDevice, &viewInfo, null, out ImageView view) != Result.Success)
            throw new Exception("Failed to create image view.");
        return view;
    }

    /// <summary>
    /// Insert a pipeline barrier to transition an image between layouts.
    /// Must be called while 'cmd' is recording.
    /// </summary>
    public void TransitionImageLayout(
        CommandBuffer cmd,
        Image image,
        Format format,
        ImageLayout oldLayout,
        ImageLayout newLayout)
    {
        PipelineStageFlags srcStage;
        PipelineStageFlags dstStage;
        AccessFlags srcAccess;
        AccessFlags dstAccess;

        if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.TransferDstOptimal)
        {
            srcAccess = 0;
            dstAccess = AccessFlags.TransferWriteBit;
            srcStage = PipelineStageFlags.TopOfPipeBit;
            dstStage = PipelineStageFlags.TransferBit;
        }
        else if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            srcAccess = AccessFlags.TransferWriteBit;
            dstAccess = AccessFlags.ShaderReadBit;
            srcStage = PipelineStageFlags.TransferBit;
            dstStage = PipelineStageFlags.FragmentShaderBit;
        }
        else if (oldLayout == ImageLayout.ShaderReadOnlyOptimal && newLayout == ImageLayout.TransferDstOptimal)
        {
            // NEW: transition back so we can overwrite the image
            srcAccess = AccessFlags.ShaderReadBit;
            dstAccess = AccessFlags.TransferWriteBit;
            srcStage = PipelineStageFlags.FragmentShaderBit;
            dstStage = PipelineStageFlags.TransferBit;
        }
        else
        {
            throw new Exception($"Unsupported layout transition: {oldLayout} → {newLayout}");
        }

        var barrier = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = oldLayout,
            NewLayout = newLayout,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = image,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1
            },
            SrcAccessMask = srcAccess,
            DstAccessMask = dstAccess
        };

        Vk.CmdPipelineBarrier(
            cmd,
            srcStage, dstStage,
            0,
            0, null,
            0, null,
            1, &barrier
        );
    }

    /// <summary>
    /// Record a copy from a linear buffer into an optimal‐tiling image.
    /// Must be called while 'cmd' is recording, and image must be in TransferDstOptimal.
    /// </summary>
    public void CopyBufferToImage(
        CommandBuffer cmd,
        Silk.NET.Vulkan.Buffer buffer,
        Image image,
        uint width,
        uint height)
    {
        var region = new BufferImageCopy
        {
            BufferOffset = 0,
            BufferRowLength = 0,
            BufferImageHeight = 0,
            ImageSubresource = new ImageSubresourceLayers
            {
                AspectMask = ImageAspectFlags.ColorBit,
                MipLevel = 0,
                BaseArrayLayer = 0,
                LayerCount = 1
            },
            ImageOffset = new Offset3D { X = 0, Y = 0, Z = 0 },
            ImageExtent = new Extent3D { Width = width, Height = height, Depth = 1 }
        };

        Vk.CmdCopyBufferToImage(
            cmd,
            buffer,
            image,
            ImageLayout.TransferDstOptimal,
            1,
            &region
        );
    }
}