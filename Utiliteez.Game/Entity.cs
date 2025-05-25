using System.Numerics;
using System.Runtime.CompilerServices;
using Utiliteez.Game;
using Utiliteez.RenderEngine.Structs;

namespace Utiliteez.RenderEngine.Objects;

public abstract class Entity
{
    private World _world;
    private Client _client;
    protected string _modelPath;

    public Entity() { }

    public void Init(int entityId, World world, Client client)
    {
        Id = entityId;
        _world = world;
        _client = client;
        // GetModel = GenerateModel;
    }
    
    public int Id { get; set; }
    public Func<(Model, Vector3)> GetModel;
    public Layers Layer;
    
    // // model, rotation
    // public virtual (Model, Vector3) GenerateModel()
    // {
    //     return (_client.GetModel(_modelPath), new Vector3());
    // }
}