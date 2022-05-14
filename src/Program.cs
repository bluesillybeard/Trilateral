using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

using Voxelesque.Render;
using Voxelesque.Render.GL33;

using System;

using libvmodel;
namespace Voxelesque
{
    public static class Program
    {

        static double time;
        static IRender render;
        //static IRenderMesh mesh;

        //static IRenderTexture texture;

        static RenderEntityModel model;
        static VMesh cpuMesh;
        static IRenderShader shader;
        static IRenderShader cameralessShader;


        static IRenderEntity entity;

        static IRenderTexture grass;
        static IRenderTexture ascii;

        static Random random;

        static RenderCamera camera;
        private static void Main()
        {
            System.Threading.Thread.CurrentThread.Name = "Main Thread";

            
            random = new Random();

            render = new GL33Render(); //todo: make a method that creates the most appropiate Render.

            render.Init(new RenderSettings()); //todo: use something other than the default settings

            render.OnVoxelesqueUpdate += new System.Action<double>(update); //subscribe the the voxelesque update event

            //initial loading stuff here - move to update method when loading bar is added

            VModel cpuModel = new VModel("Resources/vmf/models", "GrassCube.vmf", out var ignored, out var errors);
            cpuMesh = cpuModel.mesh;

            if(errors != null)RenderUtils.printErrLn(string.Join("/n", errors));
            model = render.LoadModel(cpuModel);
            shader = render.LoadShader("Resources/Shaders/");
            cameralessShader = render.LoadShader("Resources/Shaders/cameraless");
            entity = render.SpawnEntity(new EntityPosition(
                Vector3.Zero,
                Vector3.Zero,
                Vector3.One
            ), shader, model.mesh, model.texture);
            ascii = render.LoadTexture("Resources/ASCII-Extended.png");
            render.SpawnTextEntity(new EntityPosition(Vector3.Zero,Vector3.Zero,Vector3.One/20), "testing 1 2 3", true, true, cameralessShader, ascii);

            grass = model.texture;


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
            MouseState mouse = render.Mouse();
            //between -1 and 1
            Vector2 normalizedCursorPos = new Vector2(mouse.Position.X / render.WindowSize().X, mouse.Position.Y / render.WindowSize().Y) * 2 -Vector2.One;
            if (input.IsKeyReleased(Keys.C))
            {
                render.CursorLocked  = !render.CursorLocked;
            }

            if(input.IsKeyDown(Keys.F)){
                render.SpawnTextEntity(new EntityPosition(camera.Position - Vector3.UnitY, Vector3.Zero, Vector3.One), "testing 1 2 3", true, true, shader, ascii);
                render.SpawnEntity(new EntityPosition(camera.Position - Vector3.UnitY, new Vector3(Random.Shared.NextSingle(), Random.Shared.NextSingle(), Random.Shared.NextSingle()), Vector3.One), shader, model.mesh, model.texture); 
            }
            if(mouse.IsButtonDown(MouseButton.Left)){
                IRenderEntity toDelete = null;
                foreach(IRenderEntity entity in render.GetEntities()){
                    if(entity != null && RenderUtils.MeshCollides(cpuMesh, normalizedCursorPos, entity.GetTransform() * camera.GetTransform())){
                        toDelete = entity;
                        break;
                    }
                }
                if(toDelete != null)render.DeleteEntity(toDelete);
            }
            Vector3 cameraInc = new Vector3();
            if (input.IsKeyDown(Keys.W)) {
                cameraInc.Z = -1;
            } else if (input.IsKeyDown(Keys.S)) {
                cameraInc.Z = 1;
            }
            if (input.IsKeyDown(Keys.A)) {
                cameraInc.X = -1;
            } else if (input.IsKeyDown(Keys.D)) {
                cameraInc.X = 1;
            }
            if (input.IsKeyDown(Keys.LeftControl)) {
                cameraInc.Y = -1;
            } else if (input.IsKeyDown(Keys.Space)) {
                cameraInc.Y = 1;
            }
            // Update camera position
            float CAMERA_POS_STEP = 1f / 6f;
            if(input.IsKeyDown(Keys.LeftShift)) CAMERA_POS_STEP = 1f;
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
