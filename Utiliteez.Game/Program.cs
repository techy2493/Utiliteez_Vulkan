using Autofac;
using Silk.NET.GLFW;
using Silk.NET.Windowing;
using Utiliteez.RenderEngine;
using Utiliteez.RenderEngine.Extensions;
using Utiliteez.RenderEngine.Structs;
using MouseButton = Utiliteez.RenderEngine.MouseButton;

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
        var window = DIContainer.Resolve<IWindowManager>();
        var camera = DIContainer.Resolve<ICameraManager>();
        var input = DIContainer.Resolve<IInputManager>();
        
        renderEngine.RegisterModel("ground", "Models/Ground.obj");
        renderEngine.RegisterModel("powerpole", "Models/PowerPole2.obj");
        
        
        renderEngine.FinalizeAssets();

        
        var drawOrders = new List<DrawOrder>
        {
            new DrawOrder
            {
                model = "ground",
                position = [new(0, 0, 0), new(1, 0, 0), new(2, 0, 0)],
            },
            new DrawOrder
            {
                model = "powerpole",
                position = [new(0, 0, 0),new(1, 0, 0),new(0, 0, 1)],
            },
            new DrawOrder
            {
                model = "powerpole",
                position = [new(0, 2, 0),new(1, 2, 0),new(0, 2, 1)],
            },
        };
        
        input.OnMouseButtonPressed += e =>
        {
            if (e.Button == MouseButton.Right)
            {
                drawOrders.Add(new DrawOrder
                {
                    model = "powerpole",
                    position = [e.WorldPosition],
                });
                Console.WriteLine($"Right mouse button clicked at {e.WorldPosition}");
            }
        };

        const double targetFPS = 60;
        const double dt        = 1.0/targetFPS;
        double       prevTime  = renderEngine.GetTime();
        double       accumulator = 0.0;
        
        while (!window.ShouldClose)
        {
            double now       = renderEngine.GetTime();
            double frameTime = now - prevTime;
            prevTime         = now;
            accumulator     += frameTime;
            
            window.Window.DoEvents();
            
            while (accumulator >= dt)
            {
                Update((float)dt);       // your game logic
                accumulator -= dt;
            }
            
            window.Window.SwapBuffers();
            
            renderEngine.RenderScene(drawOrders.ToArray());
            //ThrottleTo(60);
        }
        
        void ThrottleTo(int fps)
        {
            double targetFrameTime = 1.0 / fps;
            double actual          = renderEngine.GetTime() - prevTime;
            double sleepTime       = targetFrameTime - actual;
            if (sleepTime > 0)
                Thread.Sleep((int)(sleepTime * 1000)); 
        }
        
        void Update(float dt)
        {
            // Handle input and update game state here
         //   renderEngine.HandleInput(dt);
            camera.HandleInput(dt);
            // For example, update camera position based on input
            // camera.Update(dt);
        }
        
        
    }
}