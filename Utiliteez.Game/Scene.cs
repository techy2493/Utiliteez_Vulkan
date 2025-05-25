using System.Numerics;
using System.Text;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Utiliteez.Game;
using Utiliteez.Game.Systems;
using Utiliteez.RenderEngine.Structs;


namespace Utiliteez.RenderEngine.Objects;

public class Scene (
    uint Height,
    uint Width,
    string Name,
    string Description,
    string Author)
{
    public World World;
    
    public void Init(Client client)
    {
        World = new World(client, Height, Width);
    }
    
    
}