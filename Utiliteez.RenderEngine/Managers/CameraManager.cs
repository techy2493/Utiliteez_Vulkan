using System.Numerics;

namespace Utiliteez.RenderEngine;

public class CameraManager(IInputManager InputManager, ISwapChainManager SwapChainManager)
{
    public Vector3 Target    = new Vector3(0, 1.5f, 0);
    public float   Distance  = 2f;
    public float   Yaw       = MathF.PI * 5f / 4f;               // fixed iso yaw
    public float   Elevation = MathF.Atan(1f / MathF.Sqrt(2f)); // fixed iso elevation

    public float ViewWidth = 6f;
    public float NearZ     = -50f;
    public float FarZ      =  50f;

    public Matrix4x4 View  => BuildView();
    public Matrix4x4 Proj  => BuildProj();
    
    private float HorizontalAxis => InputManager.KeysDown[(int)Keycode.D] - InputManager.KeysDown[(int)Keycode.A]
                                            + InputManager.KeysDown[(int)Keycode.Right] - InputManager.KeysDown[(int)Keycode.Left];
    private float VerticalAxis   => InputManager.KeysDown[(int)Keycode.W] - InputManager.KeysDown[(int)Keycode.S]
                                            + InputManager.KeysDown[(int)Keycode.Up] - InputManager.KeysDown[(int)Keycode.Down];

    private Matrix4x4 BuildProj()
    {
        float aspect = SwapChainManager.SwapChainExtent.Height
                       / (float)SwapChainManager.SwapChainExtent.Width;
        float viewH  = ViewWidth * aspect;
        return Matrix4x4.CreateOrthographicOffCenter(
            -ViewWidth * 0.5f, +ViewWidth * 0.5f,
            -viewH     * 0.5f, +viewH     * 0.5f,
            NearZ, FarZ
        );
    }

    private Matrix4x4 BuildView()
    {
        // compute the same dir vector you had:
        Vector3 dir = new Vector3(
            MathF.Cos(Elevation) * MathF.Cos(Yaw),
            MathF.Sin(Elevation),
            MathF.Cos(Elevation) * MathF.Sin(Yaw)
        );

        Vector3 eye = Target + dir * Distance;
        Vector3 up  = Vector3.UnitY;    // typically (0,1,0)

        return Matrix4x4.CreateLookAt(eye, Target, up);
    }
    
    void HandleInput(float dt)
    {;

        // ground-plane right vector (yaw rotated)
        Vector3 forward = Vector3.Normalize(new Vector3(
            MathF.Cos(Yaw + MathF.PI/2f), 0, MathF.Sin(Yaw + MathF.PI/2f)
        ));
        Vector3 right   = Vector3.Normalize(new Vector3(
            MathF.Cos(Yaw), 0, MathF.Sin(Yaw)
        ));

        float speed = 5f; // units per second

        // move target: right = positive X direction of camera, forward = perpendicular
        Target += ( right * HorizontalAxis + forward * VerticalAxis ) * speed * dt;
    }
}