namespace Voxelesque.Game;

using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

using VRender;

using VRender.Interface;

using VRender.Utility;

using System;

using vmodel;

using BasicGUI;

using World;
using World.ChunkGenerators;
using Utility;
public sealed class Voxelesque
{
    DateTime time;
    IRenderTexture ascii;
    Random random;
    Camera camera;
    Matrix4 previousCameraTransform;
    BasicGUIPlane gui;
    TextElement debug;
    TimeSpan frameDelta;
    ChunkManager chunks;
    IRenderShader chunkShader;
    public Voxelesque()
    {
        var render = VRenderLib.Render;
        var size = render.WindowSize();
        {
            var asciiOrNull = render.LoadTexture("Resources/ASCII.png", out var exception);
            if(asciiOrNull is null)
            {
                //This is so that we can keep the stack trace of the original exception
                throw new Exception("", exception);
            }
            ascii = asciiOrNull;
        }
        chunkShader = render.GetShader(new ShaderFeatures(ChunkRenderer.chunkAttributes, true, true));
        gui = new BasicGUIPlane(size.X, size.Y, new RenderDisplay(ascii));
        random = new Random();
        camera = new Camera(Vector3.Zero, Vector3.Zero, 90, size);
        debug = new TextElement(new LayoutContainer(gui.GetRoot(), VAllign.top, HAllign.left), 0xFFFFFFFF, 10, "", ascii, gui.GetDisplay(), 0);
        render.OnUpdate += Update;
        render.OnDraw += Render;
        VModel dirt;
        IRenderTexture dirtTexture;
        {
            var dirtOrNothing = VModelUtils.LoadModel("Resources/models/dirt/model.vmf", out var errors);
            if(dirtOrNothing is null)
            {
                if(errors is not null)
                {
                    System.Console.Error.WriteLine(string.Join(',', errors));
                }
                throw new Exception("Couldn't find dirt model");
            }
            dirt = dirtOrNothing.Value;
            dirtTexture = render.LoadTexture(dirt.texture);
        }
        VModel glass;
        IRenderTexture glassTexture;
        {
            var glassOrNothing = VModelUtils.LoadModel("Resources/models/glass/model.vmf", out var errors);
            if(glassOrNothing is null)
            {
                if(errors is not null)
                {
                    System.Console.Error.WriteLine(string.Join(',', errors));
                }
                throw new Exception("Couldn't find glass model");
            }
            glass = glassOrNothing.Value;
            glassTexture = render.LoadTexture(glass.texture);
        }
        FastNoiseLite noise = new FastNoiseLite(1823);
        noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise.SetFrequency(0.004f);
        noise.SetFractalOctaves(5);
        noise.SetFractalLacunarity(2.0f);
        noise.SetFractalGain(0.5f);
        chunks = new ChunkManager(new BasicChunkGenerator(
            new Block(dirt, dirtTexture, chunkShader, "dirt"),
            noise
        ));
    }
    void Update(TimeSpan delta){
        time += delta;
        Block? b = chunks.GetBlock(MathBits.GetBlockPos(camera.Position));
        string block = "none";
        if(b is not null)
        {
            block = b.name;
        }
        debug.SetText(
            "Camera Position: " + camera.Position + '\n'
            + "Camera Rotation: " + camera.Rotation + '\n'
            + "FPS: " + (int)(1/(frameDelta.Ticks/10_000_000d)) + '\n'
            + "UPS: " + (int)(1/(delta.Ticks/10_000_000d)) + '\n'
            + "block:" + block
        );

        UpdateCamera(delta);
        chunks.Update(camera.Position, 150);
        gui.Iterate();
        Vector2i size = VRenderLib.Render.WindowSize();
        gui.SetSize(size.X, size.Y);
    }

    void Render(TimeSpan delta){
        VRenderLib.Render.BeginRenderQueue();
        chunks.Draw(camera);
        gui.Draw();
        VRenderLib.Render.EndRenderQueue();
        frameDelta = delta;
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
        if(keyboard.IsKeyDown(Keys.LeftAlt)) cameraSpeed = 1f/15f;
        camera.Move(cameraInc * cameraSpeed);
        // Update camera based on mouse
        float sensitivity = 0.5f;
        if (VRenderLib.Render.CursorLocked || mouse.IsButtonDown(MouseButton.Right)) {
            camera.Rotation += new Vector3((mouse.Y - mouse.PreviousY) * sensitivity, (mouse.X - mouse.PreviousX) * sensitivity, 0);
        }
        camera.SetAspect(VRenderLib.Render.WindowSize());
    }
}