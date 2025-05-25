using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using Utiliteez.RenderEngine.Interfaces;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Utiliteez.RenderEngine;

public interface ISwapChainManager
{
    ref readonly KhrSwapchain KhrSwapChain { get; }
    ref readonly SwapchainKHR SwapChainKhr { get; }
    Image[]? SwapChainImages { get; }
    Format SwapChainImageFormat { get; }
    Extent2D SwapChainExtent { get; }
    ImageView[]? SwapChainImageViews { get; }
    ref readonly Semaphore ImageAvailableSemaphore { get; }
    ref readonly Semaphore RenderFinishedSemaphores { get; }
    ref readonly Fence InFlightFences { get; }
    ref readonly ImageView DepthImageView { get; }
    IDeviceManager DeviceManager { get; init; }
    ISurfaceManager SurfaceManager { get; init; }
    Vk Vk { get; init; }
    IInstanceManager InstanceManager { get; init; }
    IWindowManager WindowManager { get; init; }
    void Initialize();
}