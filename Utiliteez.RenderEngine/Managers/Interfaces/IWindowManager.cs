using Silk.NET.Windowing;

namespace Utiliteez.RenderEngine;

public interface IWindowManager
{
    IWindow Window { get; }
    void CreateWindow();
    void Run();
    bool Close();
    bool ShouldClose { get; }
}