//This is the screen where most of the game is played.
namespace Trilateral.Game.Screen;
using System;
using BasicGUI;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Trilateral;
using Trilateral.Utility;
using Trilateral.World;
using Trilateral.World.ChunkGenerators;
using VRenderLib;
using VRenderLib.Interface;

public sealed class MainGameScreen : IScreen
{
    TextElement debug;
    GameWorld world;
    IRenderShader chunkShader;
    public MainGameScreen(BasicGUIPlane gui, string worldName)
    {
        var font = Program.Game.MainFont;
        var staticProperties = Program.Game.StaticProperties;
        var settings  = Program.Game.Settings;
        var blockRegistry =  Program.Game.BlockRegistry;
        var render = IRender.CurrentRender;
        chunkShader = render.GetShader(new ShaderFeatures(ChunkRenderer.chunkAttributes, true, true));
        debug = new TextElement(new LayoutContainer(gui.GetRoot(), VAllign.top, HAllign.left), 0xFFFFFFFF, 10, "", font, gui.GetDisplay(), 0);
        world = new GameWorld(
            staticProperties.pathToConfig + "/saves/" + worldName,
            "trilateral:simple"
        , settings.renderThreadsMultiplier, settings.worldThreadsMultiplier);
    }
    public IScreen? Update(TimeSpan delta, BasicGUIPlane gui)
    {
        Profiler.PushRaw("DebugText");
        Block? b = world.chunkManager.GetBlock(MathBits.GetBlockPos(world.camera.Position) + MathBits.GetChunkBlockPos(world.playerChunk));
        string block = "none";
        if(b is not null)
        {
            block = b.name;
        }
        //TODO: multiple debug menus
        // ALSO TODO: realtime performance profile chart, like Minecraft's pie chart.
        debug.SetText(
              "FPS: " + (int)(1/(Program.Game.FrameDelta.Ticks/(double)TimeSpan.TicksPerSecond)) + '\n'
            + "UPS: " + (int)(1/(delta.Ticks/(double)TimeSpan.TicksPerSecond)) + '\n'
            + "Player Position: " + world.camera.Position + '\n'
            + "Player chunk: " + world.playerChunk + "\n"
            + "Camera Rotation: " + world.camera.Rotation + '\n'
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
        world.Update();
        return this;
    }

    void UpdatePlayer(TimeSpan delta)
    {
        KeyboardState keyboard = VRender.Render.Keyboard();
        MouseState mouse = VRender.Render.Mouse();
        //Place a block if the player preses a button
        if (keyboard.IsKeyDown(Keys.E))
        {
            world.chunkManager.TrySetBlock(Program.Game.BlockRegistry["trilateral:glassBlock"], MathBits.GetBlockPos(world.camera.Position) + MathBits.GetChunkBlockPos(world.playerChunk));
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
        world.camera.Move(cameraInc * cameraSpeed);
        // Update camera based on mouse
        float sensitivity = 0.5f;
        if (VRender.Render.CursorLocked || mouse.IsButtonDown(MouseButton.Right)) {
            world.camera.Rotation += new Vector3((mouse.Y - mouse.PreviousY) * sensitivity, (mouse.X - mouse.PreviousX) * sensitivity, 0);
        }
        world.camera.SetAspect(VRender.Render.WindowSize());
        Vector3i cameraChunk = MathBits.GetChunkPos(world.camera.Position);
        Vector3 cameraChunkWorldPos = MathBits.GetChunkWorldPosUncentered(cameraChunk);
        Vector3 residual = world.camera.Position - cameraChunkWorldPos;
        world.camera.Position = residual;
        world.playerChunk += cameraChunk;
    }
    public void Draw(TimeSpan delta)
    {
        world.Draw();
    }

    public void OnExit()
    {
        world.Dispose();
    }
}