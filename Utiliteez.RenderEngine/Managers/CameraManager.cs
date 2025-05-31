
using System.Numerics;

namespace Utiliteez.RenderEngine;

public class CameraManager(
    ISwapChainManager SwapChainManager
) : ICameraManager
{
    public Vector3 Target = new Vector3(0, 1.5f, 0);
    public float Distance = 2f;
    public float Yaw = MathF.PI * 5f / 4f;
    public float Elevation = MathF.Atan(1f / MathF.Sqrt(2f));

    public float ViewWidth = 6f;
    public float NearZ = -50f;
    public float FarZ = 50f;

    public IInputManager InputManager { get; private set; }
    
    private const float PanSpeed = 5f;

    public void BindInput(IInputManager inputManager)
    {
        InputManager = inputManager;
    }
    
    public Matrix4x4 View => BuildView();
    public Matrix4x4 Proj => BuildProj();

    private float ButtonValue(Keycode key)
        => InputManager.KeysDown[(int)key] > 0f ? 1f : 0f;
    
    private float HorizontalAxis
    {
        get
        {
            if (InputManager == null)
                return 0f; // No input manager initialized
            float right = ButtonValue(Keycode.D) + ButtonValue(Keycode.Right);
            float left = ButtonValue(Keycode.A) + ButtonValue(Keycode.Left);
            return right - left;
        }
    }

    private float VerticalAxis
    {
        get
        {
            if (InputManager == null)
                return 0f; // No input manager initialized
            float up = ButtonValue(Keycode.W) + ButtonValue(Keycode.Up);
            float down = ButtonValue(Keycode.S) + ButtonValue(Keycode.Down);
            return up - down;
        }
    }

    private Matrix4x4 BuildProj()
    {
        float aspect = SwapChainManager.SwapChainExtent.Height
                       / (float)SwapChainManager.SwapChainExtent.Width;
        float viewH = ViewWidth * aspect;

        return Matrix4x4.CreateOrthographicOffCenter(
            -ViewWidth * 0.5f, +ViewWidth * 0.5f,
            -viewH * 0.5f, +viewH * 0.5f,
            NearZ, FarZ
        );
    }

    private Matrix4x4 BuildView()
    {
        float cosE = MathF.Cos(Elevation);
        Vector3 dir = new Vector3(
            cosE * MathF.Cos(Yaw),
            MathF.Sin(Elevation),
            cosE * MathF.Sin(Yaw)
        );

        Vector3 eye = Target + dir * Distance;
        return Matrix4x4.CreateLookAt(eye, Target, new Vector3(0, -1, 0));
    }

    public void HandleInput(double dt)
    {
        Vector3 right = new Vector3(MathF.Cos(Yaw), 0, MathF.Sin(Yaw));
        Vector3 forward = new Vector3(-MathF.Sin(Yaw), 0, MathF.Cos(Yaw));

        float h = HorizontalAxis;
        float v = VerticalAxis * -1;

        Vector3 delta = (right * v + forward * h) * PanSpeed * (float)dt;
        Target += delta;
    }
    /// <summary>
/// Convert a mouse‐click at pixel (mouseX, mouseY) in the GLFW window
/// into a world‐space point on the ground plane Y = 0. Returns null if no hit.
/// Horizontal movement (ΔX) → shift along cameraRight (XZ),
/// Vertical movement (ΔY)   → shift along groundForward (XZ).
/// </summary>
public Vector3? ScreenToGround(double DmouseX, double DmouseY)
{
    float mouseX = (float)DmouseX;
    float mouseY = (float)DmouseY;

    // ─── 1) Get viewport dimensions ─────────────────────────────────────────
    float windowWidth  = SwapChainManager.SwapChainExtent.Width;
    float windowHeight = SwapChainManager.SwapChainExtent.Height;

    // ─── 2) Convert pixel coords → Normalized Device Coordinates (NDC) ─────────
    //     NDC X ∈ [–1..+1], left→–1, right→+1
    //     NDC Y ∈ [–1..+1], bottom→–1, top→+1   (so we flip Y with the “1 –”)
    float ndcX = (mouseX  / windowWidth ) * 2f - 1f;  
    float ndcY = 1f - (mouseY / windowHeight) * 2f;

    // ─── 3) Compute half‐extents of orthographic frustum in camera space ────
    //     We built the ortho with ViewWidth, and height = ViewWidth * (h/w)
    float aspect   = windowHeight / windowWidth;
    float halfCamW = ViewWidth * 0.5f;         // half‐width along camera’s X
    float halfCamH = halfCamW * aspect;        // half‐height along camera’s Y

    // ─── 4) Map NDC → camera‐space offsets on the “view plane” ───────────────
    float camOffsetX = ndcX * halfCamW; // how far right/left in camera X
    float camOffsetY = ndcY * halfCamH; // how far up/down   in camera Y

    // ─── 5) Reconstruct the camera’s orientation exactly as BuildView() ───────
    float cosE = MathF.Cos(Elevation);
    Vector3 dir = new Vector3(
        cosE * MathF.Cos(Yaw),
        MathF.Sin(Elevation),
        cosE * MathF.Sin(Yaw)
    );
    Vector3 eye = Target + dir * Distance;

    // In a left‐handed CreateLookAt(eye, Target, up=(0, -1, 0)):
    //    world‐space forward = Normalize(Target - eye)
    Vector3 cameraForward = Vector3.Normalize(Target - eye);

    // Use a fixed “world up” = (0, 1, 0) to build ground‐plane basis:
    Vector3 worldUp = Vector3.UnitY;

    // cameraRight = normalize(cross(worldUp, cameraForward))
    Vector3 cameraRight = Vector3.Normalize(Vector3.Cross(worldUp, cameraForward));

    // groundForward = normalize(cross(cameraRight, worldUp))
    // This lies entirely in XZ, pointing “forward” on the ground.
    Vector3 groundForward = Vector3.Normalize(Vector3.Cross(cameraRight, worldUp));

    // ─── 6) Compute the ray’s origin on the view plane ─────────────────────────
    // The center of ortho view (NDC=(0,0)) sits at “Target.”
    // Shift it by camOffsetX along cameraRight, and by camOffsetY along groundForward.
    Vector3 rayOrigin = Target
                      + cameraRight   * camOffsetX
                      + groundForward * camOffsetY;

    // ─── 7) Ray direction is always cameraForward (ortho → parallel rays) ─────
    Vector3 rayDir = cameraForward;

    // ─── 8) Intersect this ray with the ground plane Y = 0 ────────────────────
    // Solve: rayOrigin.Y + t * rayDir.Y = 0  →  t = -rayOrigin.Y / rayDir.Y
    if (MathF.Abs(rayDir.Y) < 1e-6f)
    {
        // Ray is nearly parallel to the ground plane—no valid intersection.
        return null;
    }

    float t = -rayOrigin.Y / rayDir.Y;
    if (t < 0f)
    {
        // Intersection is “behind” the camera’s view‐plane; ignore.
        return null;
    }

    Vector3 worldHit = rayOrigin + rayDir * t;
    return worldHit;  // Y should be ≈ 0
}
}