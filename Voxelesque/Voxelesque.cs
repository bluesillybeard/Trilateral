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

//TODO: The framerate still drops when loading chunks.
// I have two guesses: We are still waiting for it all to finish befoer starting the next frame,
// Or it's blocking somewhere else and I have to track the blocking code down and remove it.
// The profiler can help, but it's not perfect.
public sealed class Voxelesque
{
    DateTime start;
    DateTime time;
    IRenderTexture ascii;
    Random random;
    //We keep the camera position small, and use a chunk's position.
    Vector3i playerChunk;
    Camera camera;
    Matrix4 previousCameraTransform;
    BasicGUIPlane gui;
    TextElement debug;
    TimeSpan frameDelta;
    ChunkManager chunks;
    IRenderShader chunkShader;
    uint totalFrames;
    public Voxelesque()
    {
        start = DateTime.Now;
        time = DateTime.Now;
        var render = VRender.Render;
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
        camera = new Camera(new Vector3(0f, 10f, 0f), Vector3.Zero, 90, size);
        debug = new TextElement(new LayoutContainer(gui.GetRoot(), VAllign.top, HAllign.left), 0xFFFFFFFF, 10, "", ascii, gui.GetDisplay(), 0);
        render.OnUpdate += Update;
        render.OnDraw += Render;
        render.OnCleanup += Dispose;
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
        if(!firstFrame)Profiler.Pop("Wait");
        Profiler.Push("Update");
        Profiler.Push("DebugText");
        time += delta;
        Block? b = chunks.GetBlock(MathBits.GetBlockPos(camera.Position));
        string block = "none";
        if(b is not null)
        {
            block = b.name;
        }
        debug.SetText(
            "Player Position: " + camera.Position + '\n'
             + "Player chunk: " + playerChunk + "\n"
            + "Camera Rotation: " + camera.Rotation + '\n'
            + "FPS: " + (int)(1/(frameDelta.Ticks/(double)TimeSpan.TicksPerSecond)) + '\n'
            + "UPS: " + (int)(1/(delta.Ticks/(double)TimeSpan.TicksPerSecond)) + '\n'
            + "block:" + block
        );
        Profiler.Pop("DebugText");

        UpdateCamera(delta);
        chunks.Update(camera.Position + MathBits.GetChunkWorldPosUncentered(playerChunk), 200);
        Profiler.Push("GUIIterate");
        gui.Iterate();
        Vector2i size = VRender.Render.WindowSize();
        gui.SetSize(size.X, size.Y);
        Profiler.Pop("GUIIterate");
        Profiler.Pop("Update");
        if(!firstFrame)Profiler.Push("Wait");
    }

    bool firstFrame = true;
    void Render(TimeSpan delta){
        if(!firstFrame)Profiler.Pop("Wait");
        Profiler.Push("Render");
        totalFrames++;
        try{
            Profiler.Push("RenderChunks");
            chunks.Draw(camera, playerChunk);
            Profiler.Pop("RenderChunks");
            Profiler.Push("RenderGUI");
            gui.Draw();
            Profiler.Pop("RenderGUI");
            VRender.Render.EndRenderQueue();
            frameDelta = delta;
        } catch (Exception e)
        {
            Console.Error.WriteLine("Error rendering chunk: " + e.Message + "\ntacktrace: " + e.StackTrace);
        }
        Profiler.Pop("Render");
        Profiler.Push("Wait");
        firstFrame = false;
    }

    void UpdateCamera(TimeSpan delta)
    {
        previousCameraTransform = camera.GetTransform();
        KeyboardState keyboard = VRender.Render.Keyboard();
        MouseState mouse = VRender.Render.Mouse();
        if (keyboard.IsKeyReleased(Keys.C))
        {
            VRender.Render.CursorLocked  = !VRender.Render.CursorLocked;
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
        if(keyboard.IsKeyDown(Keys.LeftShift)) cameraSpeed = 5f;
        if(keyboard.IsKeyDown(Keys.LeftAlt)) cameraSpeed = 1f/15f;
        camera.Move(cameraInc * cameraSpeed);
        // Update camera based on mouse
        float sensitivity = 0.5f;
        if (VRender.Render.CursorLocked || mouse.IsButtonDown(MouseButton.Right)) {
            camera.Rotation += new Vector3((mouse.Y - mouse.PreviousY) * sensitivity, (mouse.X - mouse.PreviousX) * sensitivity, 0);
        }
        camera.SetAspect(VRender.Render.WindowSize());
        Vector3i cameraChunk = MathBits.GetChunkPos(camera.Position);
        Vector3 cameraChunkWorldPos = MathBits.GetChunkWorldPosUncentered(cameraChunk);
        Vector3 residual = camera.Position - cameraChunkWorldPos;
        camera.Position = residual;
        playerChunk += cameraChunk;
    }


    void Dispose()
    {
        System.Console.WriteLine("average fps:" + totalFrames/((time-start).Ticks/(double)TimeSpan.TicksPerSecond));
    }
}