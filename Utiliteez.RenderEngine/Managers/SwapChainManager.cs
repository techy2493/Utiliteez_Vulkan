using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using Utiliteez.RenderEngine.Interfaces;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Utiliteez.RenderEngine;

public unsafe record SwapChainManager(
    IDeviceManager DeviceManager,
    ISurfaceManager SurfaceManager,
    Vk Vk,
    IInstanceManager InstanceManager,
    IWindowManager WindowManager) : ISwapChainManager
{
    private SwapchainKHR _swapChainKhr;
    private KhrSwapchain _khrSwapChain;
    private Semaphore _imageAvailableSemaphore;
    private Semaphore _renderFinishedSemaphores;
    private Fence _inFlightFences;

    public ref readonly KhrSwapchain KhrSwapChain => ref _khrSwapChain;
    public ref readonly SwapchainKHR SwapChainKhr => ref _swapChainKhr;

    public Image[]? SwapChainImages { get; private set; }

    public Format SwapChainImageFormat { get; private set; }

    public Extent2D SwapChainExtent { get; private set; }

    public ImageView[]? SwapChainImageViews { get; private set; }

    public ref readonly Semaphore ImageAvailableSemaphore => ref _imageAvailableSemaphore;
    public ref readonly Semaphore RenderFinishedSemaphores => ref _renderFinishedSemaphores;
    public ref readonly Fence InFlightFences => ref _inFlightFences;
    private ImageView _depthImageView;
    public ref readonly ImageView DepthImageView => ref _depthImageView; 


    public void Initialize()
    {
        CreateSwapChain();
        CreateImageViews();
        CreateSyncObjects();
    }

    public void CreateSwapChain()
    {
        var swapChainSupport = QuerySwapChainSupport(DeviceManager.PhysicalDevice);

        var surfaceFormat = ChooseSwapSurfaceFormat(swapChainSupport.Formats);
        var presentMode = ChoosePresentMode(swapChainSupport.PresentModes);
        var extent = ChooseSwapExtent(swapChainSupport.Capabilities);

        var imageCount = swapChainSupport.Capabilities.MinImageCount + 1;
        if (swapChainSupport.Capabilities.MaxImageCount > 0 && imageCount > swapChainSupport.Capabilities.MaxImageCount)
        {
            imageCount = swapChainSupport.Capabilities.MaxImageCount;
        }

        SwapchainCreateInfoKHR creatInfo = new()
        {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = SurfaceManager.SurfaceKhr,

            MinImageCount = imageCount,
            ImageFormat = surfaceFormat.Format,
            ImageColorSpace = surfaceFormat.ColorSpace,
            ImageExtent = extent,
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.ColorAttachmentBit,
        };

        var queueFamilyIndices = stackalloc[]
            { DeviceManager.Indices.GraphicsFamily!.Value, DeviceManager.Indices.PresentFamily!.Value };

        if (DeviceManager.Indices.GraphicsFamily != DeviceManager.Indices.PresentFamily)
        {
            creatInfo = creatInfo with
            {
                ImageSharingMode = SharingMode.Concurrent,
                QueueFamilyIndexCount = 2,
                PQueueFamilyIndices = queueFamilyIndices,
            };
        }
        else
        {
            creatInfo.ImageSharingMode = SharingMode.Exclusive;
        }

        creatInfo = creatInfo with
        {
            PreTransform = swapChainSupport.Capabilities.CurrentTransform,
            CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
            PresentMode = presentMode,
            Clipped = true,

            OldSwapchain = default
        };

        if (!Vk!.TryGetDeviceExtension(InstanceManager.Instance, DeviceManager.LogicalDevice, out _khrSwapChain))
        {
            throw new NotSupportedException("VK_KHR_swapchain extension not found.");
        }

        if (_khrSwapChain!.CreateSwapchain(DeviceManager.LogicalDevice, in creatInfo, null, out _swapChainKhr) !=
            Result.Success)
        {
            throw new Exception("failed to create swap chain!");
        }

        _khrSwapChain.GetSwapchainImages(DeviceManager.LogicalDevice, _swapChainKhr, ref imageCount, null);
        SwapChainImages = new Image[imageCount];
        fixed (Image* swapChainImagesPtr = SwapChainImages)
        {
            _khrSwapChain.GetSwapchainImages(DeviceManager.LogicalDevice, _swapChainKhr, ref imageCount,
                swapChainImagesPtr);
        }

        SwapChainImageFormat = surfaceFormat.Format;
        SwapChainExtent = extent;
    }

    private SwapChainSupportDetails QuerySwapChainSupport(PhysicalDevice physicalDevice) =>
        QuerySwapChainSupport(physicalDevice, SurfaceManager.SurfaceKhr, SurfaceManager.KhrSurface);

    public static SwapChainSupportDetails QuerySwapChainSupport(PhysicalDevice physicalDevice, SurfaceKHR surfaceKhr,
        KhrSurface khrSurface)
    {
        var details = new SwapChainSupportDetails();

        khrSurface.GetPhysicalDeviceSurfaceCapabilities(physicalDevice, surfaceKhr, out details.Capabilities);

        uint formatCount = 0;
        khrSurface.GetPhysicalDeviceSurfaceFormats(physicalDevice, surfaceKhr, ref formatCount, null);

        if (formatCount != 0)
        {
            details.Formats = new SurfaceFormatKHR[formatCount];
            fixed (SurfaceFormatKHR* formatsPtr = details.Formats)
            {
                khrSurface.GetPhysicalDeviceSurfaceFormats(physicalDevice, surfaceKhr, ref formatCount, formatsPtr);
            }
        }
        else
        {
            details.Formats = [];
        }

        uint presentModeCount = 0;
        khrSurface.GetPhysicalDeviceSurfacePresentModes(physicalDevice, surfaceKhr, ref presentModeCount, null);

        if (presentModeCount != 0)
        {
            details.PresentModes = new PresentModeKHR[presentModeCount];
            fixed (PresentModeKHR* formatsPtr = details.PresentModes)
            {
                khrSurface.GetPhysicalDeviceSurfacePresentModes(physicalDevice, surfaceKhr, ref presentModeCount,
                    formatsPtr);
            }
        }
        else
        {
            details.PresentModes = [];
        }

        return details;
    }


    private SurfaceFormatKHR ChooseSwapSurfaceFormat(IReadOnlyList<SurfaceFormatKHR> availableFormats)
    {
        foreach (var availableFormat in availableFormats)
        {
            if (availableFormat.Format == Format.B8G8R8A8Srgb &&
                availableFormat.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr)
            {
                return availableFormat;
            }
        }

        return availableFormats[0];
    }


    private PresentModeKHR ChoosePresentMode(IReadOnlyList<PresentModeKHR> availablePresentModes)
    {
        foreach (var availablePresentMode in availablePresentModes)
        {
            if (availablePresentMode == PresentModeKHR.MailboxKhr)
            {
                return availablePresentMode;
            }
        }

        return PresentModeKHR.FifoKhr;
    }

    private Extent2D ChooseSwapExtent(SurfaceCapabilitiesKHR capabilities)
    {
        if (capabilities.CurrentExtent.Width != uint.MaxValue)
        {
            return capabilities.CurrentExtent;
        }
        else
        {
            var framebufferSize = WindowManager.Window!.FramebufferSize;

            Extent2D actualExtent = new()
            {
                Width = (uint)framebufferSize.X,
                Height = (uint)framebufferSize.Y
            };

            actualExtent.Width = Math.Clamp(actualExtent.Width, capabilities.MinImageExtent.Width,
                capabilities.MaxImageExtent.Width);
            actualExtent.Height = Math.Clamp(actualExtent.Height, capabilities.MinImageExtent.Height,
                capabilities.MaxImageExtent.Height);

            return actualExtent;
        }
    }

    public void CreateImageViews()
    {
        SwapChainImageViews = new ImageView[SwapChainImages!.Length];

        for (int i = 0; i < SwapChainImages.Length; i++)
        {
            ImageViewCreateInfo createInfo = new()
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = SwapChainImages[i],
                ViewType = ImageViewType.Type2D,
                Format = SwapChainImageFormat,
                Components =
                {
                    R = ComponentSwizzle.Identity,
                    G = ComponentSwizzle.Identity,
                    B = ComponentSwizzle.Identity,
                    A = ComponentSwizzle.Identity,
                },
                SubresourceRange =
                {
                    AspectMask = ImageAspectFlags.ColorBit,
                    BaseMipLevel = 0,
                    LevelCount = 1,
                    BaseArrayLayer = 0,
                    LayerCount = 1,
                }
            };

            if (Vk!.CreateImageView(DeviceManager.LogicalDevice, in createInfo, null, out SwapChainImageViews[i]) !=
                Result.Success)
            {
                throw new Exception("failed to create image views!");
            }
        }


        // pick a supported depth format
        Format depthFormat = Format.D32Sfloat;

        // 1) create the VkImage
        var depthImageInfo = new ImageCreateInfo
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Format = depthFormat,
            Extent = new Extent3D { Width = SwapChainExtent.Width, Height = SwapChainExtent.Height, Depth = 1 },
            MipLevels = 1,
            ArrayLayers = 1,
            Samples = SampleCountFlags.Count1Bit,
            Tiling = ImageTiling.Optimal,
            Usage = ImageUsageFlags.DepthStencilAttachmentBit,
        };
        Image depthImage;
        Vk.CreateImage(DeviceManager.LogicalDevice,ref depthImageInfo, null, &depthImage);

        // 2) allocate & bind memory
        Vk.GetImageMemoryRequirements(DeviceManager.LogicalDevice, depthImage, out var memReq);
        var allocInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memReq.Size,
            MemoryTypeIndex = VulkanBuffer.FindMemoryType(Vk, DeviceManager.PhysicalDevice, memReq.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit),
        };
        DeviceMemory depthImageMemory;
        Vk.AllocateMemory(DeviceManager.LogicalDevice, ref allocInfo, null, &depthImageMemory);
        Vk.BindImageMemory(DeviceManager.LogicalDevice, depthImage, depthImageMemory, 0);

        // 3) create the ImageView
        var depthViewInfo = new ImageViewCreateInfo
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = depthImage,
            ViewType = ImageViewType.Type2D,
            Format = depthFormat,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.DepthBit,
                BaseMipLevel = 0, LevelCount = 1,
                BaseArrayLayer = 0, LayerCount = 1
            }
        };
        Vk.CreateImageView(DeviceManager.LogicalDevice, ref depthViewInfo, null, out _depthImageView);
    }

    internal void CreateSyncObjects()
    {
        _imageAvailableSemaphore = ImageAvailableSemaphore;
        _imageAvailableSemaphore = new Semaphore();
        _renderFinishedSemaphores = RenderFinishedSemaphores;
        _renderFinishedSemaphores = new Semaphore();
        _inFlightFences = InFlightFences;
        _inFlightFences = new Fence();

        SemaphoreCreateInfo semaphoreInfo = new()
        {
            SType = StructureType.SemaphoreCreateInfo,
        };

        FenceCreateInfo fenceInfo = new()
        {
            SType = StructureType.FenceCreateInfo,
            Flags = FenceCreateFlags.SignaledBit,
        };
        if (Vk!.CreateSemaphore(DeviceManager.LogicalDevice, in semaphoreInfo, null, out _imageAvailableSemaphore) !=
            Result.Success ||
            Vk!.CreateSemaphore(DeviceManager.LogicalDevice, in semaphoreInfo, null, out _renderFinishedSemaphores) !=
            Result.Success ||
            Vk!.CreateFence(DeviceManager.LogicalDevice, in fenceInfo, null, out _inFlightFences) != Result.Success)
        {
            throw new Exception("failed to create synchronization objects for a frame!");
        }
    }
}