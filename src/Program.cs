using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

using Voxelesque.Render;
using Voxelesque.Render.GL33;
using Voxelesque.Render.Common;

using System;
namespace Voxelesque
{
    public static class Program
    {

        static double time;
        static IRender render;
        static IRenderMesh mesh;

        static IRenderTexture texture;
        static IRenderShader shader;

        static IRenderEntity entity;

        static Random random;

        static RenderCamera camera;
        private static void Main()
        {
            System.Threading.Thread.CurrentThread.Name = "Main";
            System.Console.WriteLine("main thread: " + System.Threading.Thread.CurrentThread.Name);
            random = new Random();

            render = new GL33Render(); //todo: make a method that creates the most appropiate Render.

            render.Init(new RenderSettings()); //todo: use something other than the default settings

            render.OnVoxelesqueUpdate += new System.Action<double>(update);

            //initial loading stuff here - move to update method when loading bar is added
            mesh = render.LoadMesh(
                new float[]{
                //|-Position------|  |Texture coordinates
                   0.5f,  0.5f, 0.0f,  1.0f, 1.0f, // top right
                   0.5f, -0.5f, 0.0f,  1.0f, 0.0f, // bottom right
                  -0.5f, -0.5f, 0.0f,  0.0f, 0.0f, // bottom left
                  -0.5f,  0.5f, 0.0f,  0.0f, 1.0f  // top left
                },
                new uint[]{
                    0, 1, 3,
                    1, 2, 3
                }
            );

            texture = render.LoadTexture("Resources/container.png.vqoi");

            shader = render.LoadShader("Resources/Shaders/");

            entity = render.SpawnEntity(new EntityPosition(
                new Vector3(0, 0, 0),
                new Vector3(0, 0, 0),
                Vector3.One
            ), shader, mesh, texture);

            camera = render.SpawnCamera(new Vector3(0, 0, 0), new Vector3(0, 0, 0), 90);
            render.SetCamera(camera);
            render.Run();
        }
        static void update(double d){
            time += d;

            //entity.Position = new EntityPosition(
            //    new Vector3((float)(random.NextDouble()-0.5), (float)(random.NextDouble()-0.5), (float)(random.NextDouble()-0.5)),
            //    new Vector3((float)(random.NextDouble()-0.5), (float)(random.NextDouble()-0.5), (float)(random.NextDouble()-0.5)),
            //    Vector3.One
            //);

            KeyboardState input = render.Keyboard();
            if (input.IsKeyReleased(Keys.C))
            {
                render.CursorLocked  = !render.CursorLocked;
            }

            Vector3 cameraInc = new Vector3();
            KeyboardState keyboard = render.Keyboard();
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
            float CAMERA_POS_STEP = 1f / 6f;
            if(keyboard.IsKeyDown(Keys.LeftShift)) CAMERA_POS_STEP = 1f;
            Vector3 pos = camera.Position;
            Vector3 rot = camera.Rotation * RenderCamera.degToRad;
            if (cameraInc.Z != 0) {
                pos.X += MathF.Sin(rot.Y) * -1.0f * cameraInc.Z * CAMERA_POS_STEP;
                pos.Z += MathF.Cos(rot.Y) * cameraInc.Z * CAMERA_POS_STEP;
            }
            if (cameraInc.X != 0) {
                pos.X += MathF.Sin(rot.Y - 1.57f) * -1.0f * cameraInc.X * CAMERA_POS_STEP;
                pos.Z += MathF.Cos(rot.Y - 1.57f) * cameraInc.X * CAMERA_POS_STEP;
            }
            pos.Y += cameraInc.Y * CAMERA_POS_STEP;
            MouseState mouse = render.Mouse();


            // Update camera based on mouse
            float sensitivity = 0.01f;

            if (mouse.IsButtonDown(MouseButton.Right) || render.CursorLocked) {
                rot.X += (mouse.Y - mouse.PreviousY) * sensitivity;
                rot.Y += (mouse.X - mouse.PreviousX) * sensitivity;
            }
            //send the camera position to Render
            camera.Position = pos;
            camera.Rotation = rot * RenderCamera.radToDeg;
        }
    }
}
