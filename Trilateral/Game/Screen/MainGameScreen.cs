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
using VRenderLib.Utility;

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
        Block? b = world.chunkManager.GetBlock(MathBits.GetWorldBlockPos(world.playerPos));
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
            + "Player pos: " + world.playerPos + '\n'
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
            world.chunkManager.TrySetBlock(Program.Game.BlockRegistry["trilateral:glassBlock"], MathBits.GetWorldBlockPos(world.playerPos));
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
        //1 meter/second
        float movementSpeed = 1f;
        if(keyboard.IsKeyDown(Keys.LeftShift)) movementSpeed = 5f;
        if(keyboard.IsKeyDown(Keys.LeftAlt)) movementSpeed = 1f/10f;
        var movement = cameraInc * movementSpeed;
        WorldPos movement3d = new WorldPos();
        if (movement.Z != 0) {
            movement3d.offset.X = -MathF.Sin(world.playerRotation.Y * Camera.degToRad) * movement.Z;
            movement3d.offset.Z += MathF.Cos(world.playerRotation.Y * Camera.degToRad) * movement.Z;
        }
        if (movement.X != 0) {
            movement3d.offset.X += MathF.Cos(world.playerRotation.Y * Camera.degToRad) * movement.X;
            movement3d.offset.Z += MathF.Sin(world.playerRotation.Y * Camera.degToRad) * movement.X;
        }
        world.playerPos += movement3d;
        // Update camera based on mouse
        float sensitivity = 0.5f;
        if (VRender.Render.CursorLocked || mouse.IsButtonDown(MouseButton.Right)) {
            world.playerRotation += new Vector3((mouse.Y - mouse.PreviousY) * sensitivity, (mouse.X - mouse.PreviousX) * sensitivity, 0);
        }
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