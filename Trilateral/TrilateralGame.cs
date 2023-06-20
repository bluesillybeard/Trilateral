namespace Trilateral;

using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

using VRenderLib;

using VRenderLib.Interface;

using VRenderLib.Utility;

using System;
using System.Collections.Generic;

using vmodel;

using BasicGUI;

using World;
using World.ChunkGenerators;
using Utility;

public sealed class TrilateralGame
{
    //The dictionary of every block in the game
    public Dictionary<string, Block> BlockRegistry;
    //The settings
    public readonly Settings Settings;
    //Properties that stay the same for a given installation and version of the game
    public readonly StaticProperties StaticProperties;
    //VoidBlock, since it's the only block that will always exist no matter what.
    public readonly Block VoidBlock;
    //When the game finished loading
    public readonly DateTime Start;
    DateTime time;
    //the current game time, according to the sum of all previous update deltas
    public DateTime Time {get => time;}
    uint totalFrames;
    public uint TotalFrames {get => totalFrames;}
    TimeSpan frameDelta;
    //the delta time of the last frame
    public TimeSpan FrameDelta {get => frameDelta;}
    IRenderTexture ascii;
    Random random;
    //We keep the camera position small, and use a chunk's position.
    Vector3i playerChunk;
    Camera camera;
    Matrix4 previousCameraTransform;
    BasicGUIPlane gui;
    RenderDisplay renderDisplay;
    TextElement debug;
    //ChunkManager chunks;
    GameWorld world;
    IRenderShader chunkShader;
    public TrilateralGame(StaticProperties properties, Settings settings)
    {
        this.StaticProperties = properties;
        this.Settings = settings;
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
        renderDisplay = new RenderDisplay(ascii);
        gui = new BasicGUIPlane(size.X, size.Y, renderDisplay);
        random = new Random();
        camera = new Camera(new Vector3(0f, 10f, 0f), Vector3.Zero, 90, size);
        debug = new TextElement(new LayoutContainer(gui.GetRoot(), VAllign.top, HAllign.left), 0xFFFFFFFF, 10, "", ascii, gui.GetDisplay(), 0);
        render.OnUpdate += Update;
        render.OnDraw += Render;
        render.OnCleanup += Dispose;
        VModel grass;
        IRenderTexture grassTexture;
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
            grass = dirtOrNothing.Value;
            grassTexture = render.LoadTexture(grass.texture);
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
        BlockRegistry = new Dictionary<string, Block>();
        //TODO: load these from a file
        //Yes, a void block (empty space) is literally just a glass block that doesn't get drawn.
        VoidBlock = new Block(glass, glassTexture, chunkShader, false, "void", "trilateral:void");
        BlockRegistry.Add("trilateral:void", VoidBlock);
        BlockRegistry.Add("trilateral:grassBlock", new Block(grass, grassTexture, chunkShader, "Grass", "trilateral:grassBlock"));
        BlockRegistry.Add("trilateral:glassBlock", new Block(glass, glassTexture, chunkShader, "Glass", "trilateral:glassBlock"));
        FastNoiseLite noise = new FastNoiseLite(Random.Shared.Next());
        noise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
        noise.SetFractalType(FastNoiseLite.FractalType.Ridged);
        noise.SetFrequency(0.004f);
        noise.SetFractalOctaves(5);
        noise.SetFractalLacunarity(2.0f);
        noise.SetFractalGain(0.5f);
        world = new GameWorld(
            properties.pathToConfig + "/saves/World1/",
            new BasicChunkGenerator(
                BlockRegistry["trilateral:grassBlock"],
                noise
        ), settings.renderThreadsMultiplier, settings.worldThreadsMultiplier);

        Start = DateTime.Now;
        time = DateTime.Now;
    }
    void Update(TimeSpan delta){

        if(postUpdateActive)Profiler.PopRaw("PostUpdate");
        if(postFrameActive)Profiler.PopRaw("PostFrame");
        Profiler.PushRaw("Update");
        Profiler.PushRaw("DebugText");
        time += delta;
        Block? b = world.chunkManager.GetBlock(MathBits.GetBlockPos(camera.Position) + MathBits.GetChunkBlockPos(playerChunk));
        string block = "none";
        if(b is not null)
        {
            block = b.name;
        }
        //TODO: multiple debug menus
        // ALSO TODO: realtime performance profile chart, like Minecraft's pie chart.
        debug.SetText(
              "FPS: " + (int)(1/(frameDelta.Ticks/(double)TimeSpan.TicksPerSecond)) + '\n'
            + "UPS: " + (int)(1/(delta.Ticks/(double)TimeSpan.TicksPerSecond)) + '\n'
            + "Player Position: " + camera.Position + '\n'
            + "Player chunk: " + playerChunk + "\n"
            + "Camera Rotation: " + camera.Rotation + '\n'
            + "block:" + block + '\n'
            + "existing chunks:" + world.chunkManager.NumChunks + '\n'
            + "waiting chunks:" + world.chunkManager.renderer.WaitingChunks + '\n'
            + "building chunks:" + world.chunkManager.renderer.BuildingChunks + '\n'
            + "uploading chunks:" + world.chunkManager.renderer.UploadingChunks + '\n'
            + "drawable chunks:" + world.chunkManager.renderer.DrawableChunks + '\n'
            + "chunk section cache:" + world.chunkManager.NumChunkSections + '\n'
        );
        Profiler.PopRaw("DebugText");

        UpdatePlayer(delta);
        Profiler.PushRaw("UpdateChunks");
        world.chunkManager.Update(playerChunk, Settings.loadDistance);
        Profiler.PopRaw("UpdateChunks");
        Profiler.PushRaw("GUIIterate");
        gui.Iterate();
        Vector2i size = VRender.Render.WindowSize();
        gui.SetSize(size.X, size.Y);
        //We "draw" the GUI here.
        // RenderDisplay only collects the mesh when "drawing",
        // Nothing actually gets rendered until DrawToScreen is called.
        gui.Draw();
        Profiler.PopRaw("GUIIterate");
        Profiler.PopRaw("Update");
        Profiler.PushRaw("PostUpdate");
        postUpdateActive = true;
    }

    bool postFrameActive = false;
    bool postUpdateActive = false;
    void Render(TimeSpan delta){
        double deltaSeconds = delta.Ticks/(double)TimeSpan.TicksPerSecond;
        if(postFrameActive)Profiler.PopRaw("PostFrame");
        if(postUpdateActive)Profiler.PopRaw("PostUpdate");
        Profiler.PushRaw("Render");
        totalFrames++;
        try{
            Profiler.PushRaw("RenderChunks");
            world.chunkManager.Draw(camera, playerChunk);
            Profiler.PopRaw("RenderChunks");
            Profiler.PushRaw("RenderGUI");
            //This renders the mesh that was collected during the update method.
            renderDisplay.DrawToScreen();
            Profiler.PopRaw("RenderGUI");
            VRender.Render.EndRenderQueue();
            frameDelta = delta;
        } catch (Exception e)
        {
            Console.Error.WriteLine("Error rendering chunk: " + e.Message + "\ntacktrace: " + e.StackTrace);
        }
        Profiler.PopRaw("Render");
        Profiler.PushRaw("PostFrame");
        postFrameActive = true;
    }

    void UpdatePlayer(TimeSpan delta)
    {
        previousCameraTransform = camera.GetTransform();
        KeyboardState keyboard = VRender.Render.Keyboard();
        MouseState mouse = VRender.Render.Mouse();
        //Place a block if the player preses a button
        if (keyboard.IsKeyDown(Keys.E))
        {
            world.chunkManager.TrySetBlock(BlockRegistry["trilateral:glassBlock"], MathBits.GetBlockPos(camera.Position) + MathBits.GetChunkBlockPos(playerChunk));
        }
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
        world.chunkManager.Dispose();
        System.Console.WriteLine("average fps:" + totalFrames/((time-Start).Ticks/(double)TimeSpan.TicksPerSecond));
    }
}