namespace Voxelesque.Game;

using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

using VRender;

using VRender.Interface;

using VRender.Utility;

using System;

using vmodel;

using BasicGUI;
public sealed class Voxelesque
{
    DateTime time;
    IRenderTexture ascii;
    Random random;
    Camera camera;
    Matrix4 previousCameraTransform;
    BasicGUIPlane gui;
    TextElement debug;
    int frames;
    public Voxelesque()
    {
        var render = VRenderLib.Render;
        var size = render.WindowSize();
        var asciiOrNull = render.LoadTexture("Resources/ASCII.png", out var exception);
        if(asciiOrNull is null)
        {
            throw new Exception("", exception);
        }
        ascii = asciiOrNull;
        gui = new BasicGUIPlane(size.X, size.Y, new RenderDisplay(ascii));
        random = new Random();
        camera = new Camera(Vector3.Zero, Vector3.Zero, 90, size);
        debug = new TextElement(new LayoutContainer(gui.GetRoot(), VAllign.top, HAllign.left), 0xFFFFFFFF, 10, "", ascii, gui.GetDisplay(), 0);
        render.OnUpdate += Update;
        render.OnDraw += Render;
    }
    void Update(TimeSpan delta){
        time += delta;
        debug.SetText("Entities: " + "0" + "\n"
            + "Camera Position: " + camera.Position + '\n'
            + "Camera Rotation: " + camera.Rotation + '\n'
            + "FPS: " + (int)(frames/(delta.Ticks)/10_000_000d));
        frames = 0;

        UpdateCamera(delta);
        gui.Iterate();
        Vector2i size = VRenderLib.Render.WindowSize();
        gui.SetSize(size.X, size.Y);
    }

    void Render(TimeSpan delta){
        VRenderLib.Render.BeginRenderQueue();
        gui.Draw();
        VRenderLib.Render.EndRenderQueue();
        frames++;
    }

    void UpdateCamera(TimeSpan delta)
    {
        previousCameraTransform = camera.GetTransform();
        KeyboardState keyboard = VRenderLib.Render.Keyboard();
        MouseState mouse = VRenderLib.Render.Mouse();
        if (keyboard.IsKeyReleased(Keys.C))
        {
            VRenderLib.Render.CursorLocked  = !VRenderLib.Render.CursorLocked;
        }
        Vector3 cameraInc = new Vector3();
        if (keyboard.IsKeyDown(Keys.W)) {
            cameraInc.Z = -1;
        } else if (keyboard.IsKeyDown(Keys.S)) {
            cameraInc.Z = 1;
        }
        if (keyboard.IsKeyDown(Keys.A)) {
            cameraInc.X = -1;
        } else if (keyboard.IsKeyDown(Keys.D)) {
            cameraInc.X = 1;
        }
        if (keyboard.IsKeyDown(Keys.LeftControl)) {
            cameraInc.Y = -1;
        } else if (keyboard.IsKeyDown(Keys.Space)) {
            cameraInc.Y = 1;
        }
        // Update camera position
        float cameraSpeed = 1f / 6f;
        if(keyboard.IsKeyDown(Keys.LeftShift)) cameraSpeed = 1f;
        camera.Move(cameraInc * cameraSpeed);
        // Update camera based on mouse
        float sensitivity = 0.5f;
        if (VRenderLib.Render.CursorLocked || mouse.IsButtonDown(MouseButton.Right)) {
            camera.Rotation += new Vector3((mouse.Y - mouse.PreviousY) * sensitivity, (mouse.X - mouse.PreviousX) * sensitivity, 0);
        }
    }
}