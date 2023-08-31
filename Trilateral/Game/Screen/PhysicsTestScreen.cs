using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BasicGUI;
using BepuPhysics;
using BepuPhysics.Collidables;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Trilateral.Utility;
using vmodel;
using VRenderLib;
using VRenderLib.Interface;
using VRenderLib.Utility;

namespace Trilateral.Game.Screen;

public class PhysicsTestScreen : IScreen
{
    readonly List<BodyHandle> boxes;
    readonly RenderModel unitCube;
    readonly IRenderShader shader;
    readonly Camera camera;
    public PhysicsTestScreen(Simulation sim)
    {
        boxes = new();
        //this is the floor
        sim.Statics.Add(new StaticDescription(new System.Numerics.Vector3(0, -15f, 0), sim.Shapes.Add(new Box(2500, 30, 2500))));
        //Load a model of a unit cube.
        // TODO: actually handle this error
        unitCube = VRender.Render.LoadModel("Resources/models/unitCube/model.vmf", out _) ?? throw new Exception("ayo?");
        Console.WriteLine(unitCube.mesh.GetAttributes());
        shader = VRender.Render.GetShader(File.ReadAllText("Resources/shaders/ChunkShader/vertex.glsl"), File.ReadAllText("Resources/shaders/ChunkShader/fragment.glsl"), unitCube.mesh.GetAttributes());
        camera = new Camera(Vector3.Zero, Vector3.Zero, 90, VRender.Render.WindowSize());
    }

    private void SpawnBox(Simulation sim, System.Numerics.Vector3 pos)
    {
        var boxShape = new Box(5, 5, 5);
        var boxBodyDesc = BodyDescription.CreateDynamic(RigidPose.Identity, boxShape.ComputeInertia(1), sim.Shapes.Add(boxShape), 1e-2f);
        boxBodyDesc.Pose.Position = pos;
        boxBodyDesc.Velocity.Linear = new(0, 0, 0);
        boxes.Add(sim.Bodies.Add(boxBodyDesc));
    }
    public void Draw(TimeSpan delta, IDrawCommandQueue drawCommandQueue)
    {
        //First, draw the ground.
        // It is at 0,0,0 and no rotation so it only needs to be scaled
        drawCommandQueue.Draw(unitCube.texture, unitCube.mesh, shader, new KeyValuePair<string, object>[]{
            new KeyValuePair<string, object>("model", Matrix4.CreateTranslation(0, -15, 0) * Matrix4.CreateScale(2500, 30, 2500)),
            new KeyValuePair<string, object>("camera", camera.GetTransform())
        }, true);
        
        DrawBoxes(drawCommandQueue);
        drawCommandQueue.Finish();
    }

    private void DrawBoxes(IDrawCommandQueue drawCommandQueue)
    {
        var sim = Program.Game.physics.Sim;
        foreach(var box in boxes)
        {
            var body = sim.Bodies[box];
            var position = body.Pose.Position;
            var orientation = body.Pose.Orientation;
            var transform = Matrix4.Identity;
            // TODO: I never seem to get the transform order correct
            transform *= Matrix4.CreateFromQuaternion(new Quaternion(orientation.X, orientation.Y, orientation.Z, orientation.W));
            // TODO: get size from object instead of hard-coding it
            transform *= Matrix4.CreateScale(new Vector3(5, 5, 5));
            transform *= Matrix4.CreateTranslation(new Vector3(position.X, position.Y, position.Z));
            drawCommandQueue.Draw(unitCube.texture, unitCube.mesh, shader, new KeyValuePair<string, object>[]{
                new KeyValuePair<string, object>("model", transform),
                new KeyValuePair<string, object>("camera", camera.GetTransform())
            }, true);
        }
    }
    public IScreen? Update(TimeSpan delta, BasicGUIPlane gui)
    {
        var size = VRender.Render.WindowSize();
        camera.SetAspect(size);
        KeyboardState keyboard = VRender.Render.Keyboard();
        MouseState mouse = VRender.Render.Mouse();
        if (keyboard.IsKeyReleased(Keys.C))
        {
            VRender.Render.CursorState = VRender.Render.CursorState == CursorState.Grabbed ? CursorState.Hidden : CursorState.Grabbed;
        }
        Vector3 cameraInc = new();
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
        WorldPos movement3d = new();
        if (movement.Z != 0) {
            movement3d.offset.X = -MathF.Sin(camera.Rotation.Y * Camera.degToRad) * movement.Z;
            movement3d.offset.Z += MathF.Cos(camera.Rotation.Y * Camera.degToRad) * movement.Z;
        }
        if (movement.X != 0) {
            movement3d.offset.X += MathF.Cos(camera.Rotation.Y * Camera.degToRad) * movement.X;
            movement3d.offset.Z += MathF.Sin(camera.Rotation.Y * Camera.degToRad) * movement.X;
        }
        movement3d.offset.Y = movement.Y;
        camera.Position += movement3d.LegacyValue;
        // Update rotation based on mouse
        const float sensitivity = 0.5f;
        if (VRender.Render.CursorState == CursorState.Grabbed || mouse.IsButtonDown(MouseButton.Middle)) {
            camera.Rotation += new OpenTK.Mathematics.Vector3((mouse.Y - mouse.PreviousY) * sensitivity, (mouse.X - mouse.PreviousX) * sensitivity, 0);
        }

        if(keyboard.IsKeyReleased(Keys.E))
        {
            //Spawn new boxes on a key press
            const float distance = -10;
            SpawnBox(Program.Game.physics.Sim, new(camera.Position.X - MathF.Sin(camera.Rotation.Y * Camera.degToRad) * distance, camera.Position.Y, camera.Position.Z + MathF.Cos(camera.Rotation.Y * Camera.degToRad) * distance));
        }
        return this;
    }
    public void OnExit()
    {

    }
}