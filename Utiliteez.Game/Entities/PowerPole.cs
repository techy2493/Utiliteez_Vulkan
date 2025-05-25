using Utiliteez.RenderEngine.Objects;

namespace Utiliteez.Game.Entities;

public class PowerPole: Entity
{
    public PowerPole()
    {
        _modelPath = "Models/PowerPole1.obj";
        Layer = Layers.AboveGround;
    }
    
}