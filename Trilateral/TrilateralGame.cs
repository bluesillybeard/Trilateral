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
using System.Runtime.CompilerServices;
using Trilateral.Game.Screen;
using OpenTK.Windowing.Common;
using System.IO;
using Trilateral.Physics;

public sealed class TrilateralGame
{
    //The dictionary of every block in the game
    public readonly Dictionary<string, IBlock> BlockRegistry;
    //Registry of chunk generators.
    public readonly Dictionary<string, ChunkGeneratorRegistryEntry> ChunkGenerators;
    //The settings
    public readonly Settings Settings;
    //Properties that stay the same for a given installation and version of the game
    public readonly StaticProperties StaticProperties;
    //VoidBlock, since it's the only block that will always exist no matter what.
    public readonly IBlock AirBlock;
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
    public IRenderTexture MainFont { get; }
    readonly BasicGUIPlane gui;
    public readonly RenderDisplay renderDisplay;
    public readonly PhysicsManager physics;
    IScreen currentScreen;

    public TrilateralGame(StaticProperties properties, Settings settings)
    {
        this.StaticProperties = properties;
        this.Settings = settings;
        var render = VRender.Render;
        var size = render.WindowSize();
        MainFont = render.LoadTexture("Resources/ASCII.png", out var exception) ?? throw new Exception("Could not load font texture", exception);
        renderDisplay = new RenderDisplay(MainFont);
        gui = new BasicGUIPlane(size.X, size.Y, renderDisplay);
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
        BlockRegistry = new Dictionary<string, IBlock>();
        var chunkShader = render.GetShader(File.ReadAllText("Resources/shaders/ChunkShader/vertex.glsl"), File.ReadAllText("Resources/shaders/ChunkShader/fragment.glsl"), ChunkRenderer.chunkAttributes);
        //TODO: load these from a file
        //Yes, a void block (empty space) is literally just a glass block that doesn't get drawn.
        AirBlock = new SimpleBlock(glass, glassTexture, chunkShader, false, "Air", "trilateral:air");
        BlockRegistry.Add("trilateral:air", AirBlock);
        BlockRegistry.Add("trilateral:grassBlock", new SimpleBlock(grass, grassTexture, chunkShader, "Grass", "trilateral:grassBlock"));
        BlockRegistry.Add("trilateral:glassBlock", new SimpleBlock(glass, glassTexture, chunkShader, "Glass", "trilateral:glassBlock"));

        ChunkGenerators = new Dictionary<string, ChunkGeneratorRegistryEntry>();
        //TODO: use Reflection to get every class that extends IChunkGenerator?
        // Add a method to IChunkGenerator to get an instance of its registry so the reflection idea is even plausible.
        // (It would be really cool if an interface could have an unimplemented static method that all implementing classes must implement)
        var BasicChunkGeneratorEntry = BasicChunkGenerator.CreateEntry();
        ChunkGenerators.Add(BasicChunkGeneratorEntry.id, BasicChunkGeneratorEntry);
        currentScreen = new MainMenuScreen(gui, MainFont);
        Start = DateTime.Now;
        time = DateTime.Now;
        VRender.Render.CursorState = CursorState.Hidden;

        physics = new PhysicsManager();
    }
    void Update(TimeSpan delta)
    {
        if(postUpdateActive)Profiler.PopRaw("PostUpdate");
        if(postFrameActive)Profiler.PopRaw("PostFrame");
        Profiler.PushRaw("Update");
        time += delta;
        currentScreen = currentScreen.Update(delta, gui) ?? throw new NotImplementedException("LOL i haven't programmed a way programatically to close a VRender application");
        //the GUI is iterated on updates so keyboard/mouse input timing actually make sense
        gui.Iterate();
        Profiler.PopRaw("Update");
        Profiler.PushRaw("PostUpdate");
        postUpdateActive = true;
    }

    bool postFrameActive = false;
    bool postUpdateActive = false;
    void Render(TimeSpan delta, IDrawCommandQueue drawCommandQueue){
        if(postFrameActive)Profiler.PopRaw("PostFrame");
        if(postUpdateActive)Profiler.PopRaw("PostUpdate");
        Profiler.PushRaw("Render");
        totalFrames++;
        try{
            currentScreen.Draw(delta, drawCommandQueue);
            Profiler.PushRaw("RenderGUI");
            Vector2i size = VRender.Render.WindowSize();
            gui.SetSize(size.X, size.Y);
            //We "draw" the GUI here.
            // RenderDisplay only collects the mesh when "drawing",
            // Nothing actually gets rendered until DrawToScreen is called.
            gui.Draw();
            //Draw a little cursor thing.
            // This is mainly for my own recording purposes, because OBS doesn't capture the cursor properly for some reason
            // The lack or cursor might have something to do with Flatpak, or my compositor, or maybe even X11 itself.
            // Instead of fixing OBS, I just added my own little cursor.
            var mousePosPixels = (Vector2i) VRender.Render.Mouse().Position;
            renderDisplay.DrawLineWithThickness(mousePosPixels.X, mousePosPixels.Y, mousePosPixels.X+20, mousePosPixels.Y+20, 0xAA00AAFF, 8);
            renderDisplay.DrawLineWithThickness(mousePosPixels.X, mousePosPixels.Y, mousePosPixels.X+15, mousePosPixels.Y+30, 0xAA00AAFF, 8);
            renderDisplay.DrawLineWithThickness(mousePosPixels.X+20, mousePosPixels.Y+20, mousePosPixels.X+15, mousePosPixels.Y+30, 0xAA00AAFF, 8);
            renderDisplay.DrawLineWithThickness(mousePosPixels.X, mousePosPixels.Y, mousePosPixels.X+20, mousePosPixels.Y+20, 0xFFFFFFFF, 4);
            renderDisplay.DrawLineWithThickness(mousePosPixels.X, mousePosPixels.Y, mousePosPixels.X+15, mousePosPixels.Y+30, 0xFFFFFFFF, 4);
            renderDisplay.DrawLineWithThickness(mousePosPixels.X+20, mousePosPixels.Y+20, mousePosPixels.X+15, mousePosPixels.Y+30, 0xFFFFFFFF, 4);
            renderDisplay.DrawToScreen(drawCommandQueue);
            Profiler.PopRaw("RenderGUI");
            frameDelta = delta;
        } catch (Exception e)
        {
            Console.Error.WriteLine("Error rendering chunk: " + e.Message + "\ntacktrace: " + e.StackTrace);
        }
        Profiler.PopRaw("Render");
        Profiler.PushRaw("PostFrame");
        postFrameActive = true;
    }
    void Dispose()
    {
        System.Console.WriteLine("average fps:" + totalFrames/((time-Start).Ticks/(double)TimeSpan.TicksPerSecond));
        currentScreen.OnExit();
    }
}