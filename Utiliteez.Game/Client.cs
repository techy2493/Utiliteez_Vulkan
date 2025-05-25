using System.Numerics;
using Utiliteez.Game.Entities;
using Utiliteez.RenderEngine;
using Utiliteez.RenderEngine.Objects;
using Utiliteez.RenderEngine.Structs;

namespace Utiliteez.Game;

public class Client
{
    private Scene _currentScene;
    private ResourceRepository<Model> _modelRepository;

    
    
    public void LoadScene(string  sceneName)
    {
        _currentScene = new Scene(10, 10, sceneName, "", "");
        _currentScene.World.AddEntity(new PowerPole(), new Vector2(0, 0));
    }
    
    public Model GetModel(string path)
    {
        return _modelRepository.Request(path);
    }
    
}