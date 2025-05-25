using Autofac;
using Utiliteez.RenderEngine;
using Utiliteez.RenderEngine.Extensions;

namespace Utiliteez.Game;

class Program
{
    static void Main(string[] args)
    {
        
        
        var builder = new ContainerBuilder();

        builder.RegisterModule<RenderModule>();
        builder.RegisterInstance(new Client()).SingleInstance();
        
        var DIContainer = builder.Build();

        var renderEngine = DIContainer.Resolve<RenderEngine.RenderEngine>();
        renderEngine.Initialize();
        
        var client = DIContainer.Resolve<Client>();
        
        
    }
}