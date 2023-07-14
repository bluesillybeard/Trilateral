//#define RAYCASTDEBUG

//This is the screen where most of the game is played.
namespace Trilateral.Game.Screen;
using System;
using System.Collections.Generic;
using BasicGUI;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Trilateral;
using Trilateral.Utility;
using Trilateral.World;
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
        var mousePosPixels = (Vector2i) VRender.Render.Mouse().Position;
        Program.Game.renderDisplay.DrawLine(mousePosPixels.X, mousePosPixels.Y, mousePosPixels.X+100, mousePosPixels.Y+100, 0xFF00FFFF);
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
        float movementSpeed = 0.5f;
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
        movement3d.offset.Y = movement.Y;
        world.playerPos += movement3d;
        // Update rotation based on mouse
        float sensitivity = 0.5f;
        if (VRender.Render.CursorLocked || mouse.IsButtonDown(MouseButton.Middle)) {
            world.playerRotation += new Vector3((mouse.Y - mouse.PreviousY) * sensitivity, (mouse.X - mouse.PreviousX) * sensitivity, 0);
        }
        bool left = mouse.IsButtonDown(MouseButton.Left);
        bool right = mouse.IsButtonPressed(MouseButton.Right);
        GetSelectedBlock(2, world.playerPos, out var blockSelected, out var placePos);
        if(left || right)
        {
            if(left)
            {
                world.chunkManager.TrySetBlock(Program.Game.VoidBlock, blockSelected);
            }
            if(placePos is not null && right && Program.Game.BlockRegistry.TryGetValue("trilateral:glassBlock", out var blockToPlace))
            {
                world.chunkManager.TrySetBlock(blockToPlace, placePos.Value);
            }
        }
        

    }

    public void GetSelectedBlock(float range, WorldPos playerPos, out Vector3i blockSelected, out Vector3i? placePos)
    {
        Matrix4 cameraTransform = world.camera.GetTransform();
        placePos = null;
        var mousePosPixels = (Vector2i) VRender.Render.Mouse().Position;
        Vector2 mousePos;
        (mousePos.X, mousePos.Y) = RenderDisplay.PixelToGL(mousePosPixels.X, mousePosPixels.Y);
        Vector3i blockRange =  MathBits.GetWorldBlockPos(new WorldPos(Vector3i.Zero, new Vector3(range, range, range)));
        Vector3i playerBlockPos = MathBits.GetWorldBlockPos(playerPos);
        Vector3i playerOffsetBlockPos = MathBits.GetBlockPos(playerPos.offset);
        Vector3i playerBaseBlockPos = playerBlockPos - playerOffsetBlockPos;
        blockSelected = playerBlockPos;
        PriorityQueue<Vector3i, float> blocksInRay = new PriorityQueue<Vector3i, float>();
        for(int dbx = -blockRange.X; dbx < blockRange.X; dbx++ )
        {
            for(int dby = -blockRange.Y; dby < blockRange.Y; dby++ )
            {
                for(int dbz = -blockRange.Z; dbz < blockRange.Z; dbz++ )
                {
                    Vector3i offsetBlockPos = new Vector3i(dbx, dby, dbz) + playerOffsetBlockPos;
                    Vector3i blockPos = new Vector3i(dbx, dby, dbz) + playerBlockPos;
                    float distance = (MathBits.GetBlockWorldPos(blockPos) - playerPos).LegacyValue.Length;
                    if(distance > range)continue;
                    var block = world.chunkManager.GetBlock(offsetBlockPos);
                    if(block is null)continue; //no block -> skip

                    //We actually include empty blocks, so when we are iterating at the end we can figure out where a block would be placed
                    //if(!block.draw)continue; //empty block -> skip
                    var mesh = block.model.mesh;
                    if(mesh.indices.Length == 0) mesh = Program.Game.VoidBlock.model.mesh;

                    //Create a transform for the block mesh
                    var parity = ((offsetBlockPos.X+offsetBlockPos.Z) & 1);
                    var angle = (MathF.PI/3)*parity;
                    //TODO: calculate this offset to greater accuruacy
                    var XOffset = 0.144f*parity;

                    (var sina, var cosa) = MathF.SinCos(angle);

                    //I basically need to take the following transform code and encode it into a matrix.
                    // It was taken from ChunkBuildObject, in order to replicate the transform of a single block.
                    // transformedVertex[0] = vertex[0] *  cosa + vertex[2] * sina + bx * MathBits.XScale + XOffset;
                    // transformedVertex[1] = vertex[1]                                       + by * 0.5f;
                    // transformedVertex[2] = vertex[0] * -sina + vertex[2] * cosa + bz * 0.25f;
                    //wasn't too hard. I originally did it manually, but doing it this way is WAAY more intuitive, even if it's slightly slower.
                    Matrix4 blockTransform = Matrix4.Identity
                     * Matrix4.CreateRotationY(angle)
                     * Matrix4.CreateTranslation(offsetBlockPos.X * MathBits.XScale + XOffset, offsetBlockPos.Y * 0.5f, offsetBlockPos.Z * 0.25f);
                    Matrix4 transform = blockTransform * cameraTransform;
                    if(MathBits.MeshRaycast(mesh, transform, mousePos, out var e))
                    {
                        blocksInRay.Enqueue(blockPos, distance);
                    }
                }
            }
        }
        //Go through the list in order.
        Vector3i? last = null;
        while(blocksInRay.Count > 0)
        {
            var blockPos = blocksInRay.Dequeue();
            var block = world.chunkManager.GetBlock(blockPos);
            if(block is null)
            {
                throw new Exception("null block shouldn't happen, because null blocks shouldn't have been placed into the block raycast list in the first place!");
            }
            #if RAYCASTDEBUG
            var parity = ((blockPos.X+blockPos.Z) & 1);
            var angle = (MathF.PI/3)*parity;
            //TODO: calculate this offset to greater accuruacy
            var XOffset = 0.144f*parity;
            Matrix4 blockTransform = Matrix4.Identity
                * Matrix4.CreateRotationY(angle)
                * Matrix4.CreateTranslation((blockPos.X - playerBaseBlockPos.X) * MathBits.XScale + XOffset, (blockPos.Y - playerBaseBlockPos.Y)* 0.5f, (blockPos.Z - playerBaseBlockPos.Z) * 0.25f);
            Matrix4 transform = blockTransform * cameraTransform;
            Program.Game.renderDisplay.DrawMeshLines(block.model.mesh, transform, 0x00FFFFFF, out var e);
            if(e is not null)
            {
                System.Console.Error.WriteLine("ERROR rendering debug block mesh:" + e.Message + "\n" + e.StackTrace);
            }
            #endif
            if(block.draw)
            {
                blockSelected = blockPos;
                placePos = last;
                break;
            }
            last = blockPos;
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