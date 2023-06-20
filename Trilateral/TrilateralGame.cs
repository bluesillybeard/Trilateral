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
    public IRenderTexture MainFont {get => ascii;}
    BasicGUIPlane gui;
    RenderDisplay renderDisplay;
    IScreen currentScreen;

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
                throw new Exception("Could not load font texture", exception);
            }
            ascii = asciiOrNull;
        }
        renderDisplay = new RenderDisplay(ascii);
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
        BlockRegistry = new Dictionary<string, Block>();
        var chunkShader = render.GetShader(new ShaderFeatures(ChunkRenderer.chunkAttributes, true, true));
        //TODO: load these from a file
        //Yes, a void block (empty space) is literally just a glass block that doesn't get drawn.
        VoidBlock = new Block(glass, glassTexture, chunkShader, false, "void", "trilateral:void");
        BlockRegistry.Add("trilateral:void", VoidBlock);
        BlockRegistry.Add("trilateral:grassBlock", new Block(grass, grassTexture, chunkShader, "Grass", "trilateral:grassBlock"));
        BlockRegistry.Add("trilateral:glassBlock", new Block(glass, glassTexture, chunkShader, "Glass", "trilateral:glassBlock"));
        currentScreen = new MainMenuScreen(gui, ascii);
        Start = DateTime.Now;
        time = DateTime.Now;
    }
    void Update(TimeSpan delta)
    {
        if(postUpdateActive)Profiler.PopRaw("PostUpdate");
        if(postFrameActive)Profiler.PopRaw("PostFrame");
        Profiler.PushRaw("Update");
        time += delta;
        var nextScreen = currentScreen.Update(delta, gui);
        if(nextScreen is null)
        {
            //TODO: close game
            throw new NotImplementedException("LOL i haven't programmed a way programatically to close a VRender application");
        }
        currentScreen = nextScreen;
        Profiler.PushRaw("GUIIterate");
        Vector2i size = VRender.Render.WindowSize();
        gui.SetSize(size.X, size.Y);
        gui.Iterate();
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
            currentScreen.Draw(delta);
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
    void Dispose()
    {
        System.Console.WriteLine("average fps:" + totalFrames/((time-Start).Ticks/(double)TimeSpan.TicksPerSecond));
        currentScreen.OnExit();
    }
}