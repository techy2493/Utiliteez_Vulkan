using Autofac;
using Autofac.Core;
using Autofac.Core.Registration;
using Silk.NET.Vulkan;
using Utiliteez.RenderEngine.Interfaces;

namespace Utiliteez.RenderEngine.Extensions;

public class RenderModule: Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterInstance(new WindowManager()).As<IWindowManager>().SingleInstance();
        builder.RegisterInstance(Vk.GetApi()).As<Vk>().SingleInstance();
        
        builder
            .RegisterType<InstanceManager>()
            .As<IInstanceManager>()
            .SingleInstance();
        
        builder
            .RegisterType<SurfaceManager>()
            .As<ISurfaceManager>()
            .SingleInstance();
        
        builder
            .RegisterType<DeviceManager>()
            .As<IDeviceManager>()
            .SingleInstance();
        
        builder
            .RegisterType<SwapChainManager>()
            .As<ISwapChainManager>()
            .SingleInstance();
        
        builder
            .RegisterType<AssetManager>()
            .As<IAssetManager>()
            .SingleInstance();
        
        builder
            .RegisterType<ResourceManager>()
            .As<IResourceManager>()
            .SingleInstance();
        
        builder
            .RegisterType<PipelineManager>()
            .As<IPipelineManager>()
            .SingleInstance();

        builder
            .RegisterType<CommandManager>()
            .As<ICommandManager>()
            .SingleInstance();
        
        // Keep Last
        builder
            .RegisterType<RenderEngine>()
            .SingleInstance();

    }
}