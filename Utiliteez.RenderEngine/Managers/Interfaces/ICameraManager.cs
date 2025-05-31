using System.Numerics;

namespace Utiliteez.RenderEngine;

public interface ICameraManager
{
    Matrix4x4 View { get; }
    Matrix4x4 Proj { get; }
    void HandleInput(double dt);
    Vector3? ScreenToGround(double mouseX, double mouseY);
    public void BindInput(IInputManager inputManager);
}