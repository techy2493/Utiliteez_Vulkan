using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Windowing;

namespace Utiliteez.RenderEngine;

public interface IInstanceManager
{
    Instance Instance { get; }
    ExtDebugUtils? DebugUtils { get; }
    DebugUtilsMessengerEXT DebugMessenger { get; }
    string[] DeviceExtensions { get; }
    bool EnableValidationLayers { get; }
    string[] ValidationLayers { get; }
    Vk Vk { get; init; }
    IWindowManager Window { get; init; }
    bool CheckValidationLayerSupport();
    string[] GetRequiredInstanceExtensions();
    bool CheckDeviceExtensionsSupport(PhysicalDevice device);
    void SetupDebugMessenger();
    void Initialize();
}