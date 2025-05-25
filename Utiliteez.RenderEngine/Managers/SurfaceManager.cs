using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using Utiliteez.RenderEngine.Interfaces;

namespace Utiliteez.RenderEngine;

public unsafe record SurfaceManager(
    Vk Vk,
    IInstanceManager InstanceManager,
    IWindowManager WindowManager
    ) : ISurfaceManager
{
    private KhrSurface _khrSurface;
    public KhrSurface KhrSurface => _khrSurface;
    public SurfaceKHR SurfaceKhr { get; private set; }

    public void Initialize()
    {
        if (!Vk!.TryGetInstanceExtension<KhrSurface>(InstanceManager.Instance, out _khrSurface))
        {
            throw new NotSupportedException("KHR_surface extension not found.");
        }

        SurfaceKhr = WindowManager
            .Window!
            .VkSurface!
            .Create(InstanceManager.Instance.ToHandle(), 
                (AllocationCallbacks*)null).ToSurface();
    }
}