using Silk.NET.Vulkan;
using Utiliteez.RenderEngine.Interfaces;
using Utiliteez.RenderEngine.Structs;

namespace Utiliteez.RenderEngine;

public interface ICommandManager
{
    unsafe void RenderFrame();
    void CreateDrawcommand(DrawOrder[] drawOrder);

    Vk Vk { get; init; }
    IDeviceManager DeviceManager { get; init; }
    ISwapChainManager SwapChainManager { get; init; }
    IResourceManager ResourceManager { get; init; }
    IPipelineManager PipelineManager { get; init; }
    void Initialize();
}