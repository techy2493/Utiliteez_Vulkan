using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;

namespace Utiliteez.RenderEngine;

public interface ISurfaceManager
{
    KhrSurface KhrSurface { get; }
    SurfaceKHR SurfaceKhr { get; }
    Vk Vk { get; init; }
    IInstanceManager InstanceManager { get; init; }
    IWindowManager WindowManager { get; init; }
    unsafe void Initialize();
    bool Equals(SurfaceManager? other);
    bool Equals(object? other);
    int GetHashCode();
    void Deconstruct(out Vk Vk, out IInstanceManager  InstanceManager, out IWindowManager WindowManager);
    string ToString();
}