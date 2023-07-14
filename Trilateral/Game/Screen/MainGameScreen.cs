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
        bool left = mouse.IsButtonPressed(MouseButton.Left);
        bool right = mouse.IsButtonPressed(MouseButton.Right);
        GetSelectedBlock(2, world.playerPos, out var blockSelected, out var placePos);

        if(left || right)
        {
            if(left)
            {
                //world.chunkManager.TrySetBlock(Program.Game.VoidBlock, blockSelected);
            }
            if(placePos is not null && right && Program.Game.BlockRegistry.TryGetValue("trilateral:glassBlock", out var blockToPlace))
            {
                //world.chunkManager.TrySetBlock(blockToPlace, placePos.Value);
            }
        }
        

    }

    public void GetSelectedBlock(float range, WorldPos playerPos, out Vector3i blockSelected, out Vector3i? placePos)
    {
        placePos = null;
        var mousePosPixels = (Vector2i) VRender.Render.Mouse().Position;
        Vector2 mousePos;
        (mousePos.X, mousePos.Y) = RenderDisplay.PixelToGL(mousePosPixels.X, mousePosPixels.Y);
        Vector3i blockRange =  MathBits.GetWorldBlockPos(new WorldPos(Vector3i.Zero, new Vector3(range, range, range)));
        Vector3i playerBlockPos = MathBits.GetWorldBlockPos(playerPos);
        blockSelected = playerBlockPos;
        PriorityQueue<Vector3i, float> blocksInRay = new PriorityQueue<Vector3i, float>();
        for(int dbx = -blockRange.X; dbx < blockRange.X; dbx++ )
        {
            for(int dby = -blockRange.Y; dby < blockRange.Y; dby++ )
            {
                for(int dbz = -blockRange.Z; dbz < blockRange.Z; dbz++ )
                {
                    Vector3i blockPos = new Vector3i(dbx, dby, dbz) + MathBits.GetWorldBlockPos(playerPos);
                    var block = world.chunkManager.GetBlock(blockPos);
                    if(block is null)continue; //no block -> skip

                    //We actually include empty blocks, so when we are iterating at the end we can figure out where a block would be placed
                    //if(!block.draw)continue; //empty block -> skip
                    var mesh = block.model.mesh;
                    if(mesh.indices.Length == 0) mesh = Program.Game.VoidBlock.model.mesh;

                    //Create a transform for the block mesh
                    var parity = ((blockPos.X+blockPos.Z) & 1);
                    var angle = (MathF.PI/3)*parity;
                    //TODO: calculate this offset to greater accuruacy
                    var XOffset = 0.144f*parity;

                    (var sina, var cosa) = MathF.SinCos(angle);

                    //I basically need to take the following transform code and encode it into a matrix.
                    // It was taken from ChunkBuildObject, in order to replicate the transform of a single block.
                    // transformedVertex[0] = vertex[0] *  cosa + vertex[2] * sina + bx * MathBits.XScale + XOffset;
                    // transformedVertex[1] = vertex[1]                                       + by * 0.5f;
                    // transformedVertex[2] = vertex[0] * -sina + vertex[2] * cosa + bz * 0.25f;
                    /*
                    I used this to help guide me on how to set it up
                    (It's OpenTK Matrix4's transform method)
                    result = new Vector4(
                        vec.X * mat.Row0.X + vec.Y * mat.Row1.X + vec.Z * mat.Row2.X + vec.W * mat.Row3.X,
                        vec.X * mat.Row0.Y + vec.Y * mat.Row1.Y + vec.Z * mat.Row2.Y + vec.W * mat.Row3.Y,
                        vec.X * mat.Row0.Z + vec.Y * mat.Row1.Z + vec.Z * mat.Row2.Z + vec.W * mat.Row3.Z,
                        vec.X * mat.Row0.W + vec.Y * mat.Row1.W + vec.Z * mat.Row2.W + vec.W * mat.Row3.W
                    );
                    */
                    //wasn't too hard
                    Matrix4 blockTransform = Matrix4.Identity;
                    blockTransform = Matrix4.CreateTranslation(MathBits.XScale + XOffset, blockPos.Y * 0.5f, blockPos.Z * 0.25f) * blockTransform;
                    blockTransform = Matrix4.CreateRotationY(angle) * blockTransform;
                    //  = new Matrix4(
                    //     cosa, 0, sina,MathBits.XScale + XOffset,
                    //     0,    1, 0,   blockPos.Y * 0.5f,
                    //     -sina,0, cosa,blockPos.Z * 0.25f,
                    //     0,    0, 0,   1
                    // );
                    //blockTransform.Transpose();
                    Matrix4 cameraTransform = world.camera.GetTransform();
                    Matrix4 transform = blockTransform * cameraTransform;
                    if(MathBits.MeshRaycast(mesh, transform, Vector2.Zero, out var e))
                    {
                        float distance = (playerBlockPos - blockPos).EuclideanLength;
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
            if(block is null)continue;
            if(!block.draw || block.model.mesh.indices.Length==0)
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