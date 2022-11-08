using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

using Render;

using System;

using vmodel;
namespace Voxelesque.Game
{
    public static class Program
    {

        static double time;
        static IRender render;
        static RenderEntityModel model;
        static VMesh cpuMesh;
        static IRenderShader shader;
        static IRenderShader cameralessShader;
        static IRenderTextEntity debugText;
        static IRenderTexture grass;
        static IRenderTexture ascii;
        static Random random;

        static RemoveOnTouchBehavior GrassCubeBehavior;

        static RenderCamera camera;

        static int frames;
        private static void Main()
        {
            //Goes without saying, but the Main method here is an absolutely atrocious mess.
            //I'll fix it someday, but for now it stays.
            //TODO: fix horrible mess of a Main method
            System.Threading.Thread.CurrentThread.Name = "Main Thread";

            
            random = new Random((int)DateTime.Now.Ticks);

            render = RenderUtils.CreateIdealRender();

            render.Init(new RenderSettings()); //todo: use something other than the default settings



            //initial loading stuff here - move to update method and make asynchronous when loading bar is added

            VModel? grassCubeModel = VModelUtils.LoadModel("Resources/vmf/models/GrassCube.vmf", out var errors);

            if(errors != null)RenderUtils.PrintErrLn(string.Join("/n", errors));
            cpuMesh = grassCubeModel.Value.mesh;
            model = render.LoadModel(grassCubeModel.Value);
            shader = render.LoadShader("Resources/Shaders/", out var err0);
            if(err0 != null)RenderUtils.PrintErrLn(err0);
            cameralessShader = render.LoadShader("Resources/Shaders/cameraless", out var err);
            if(err != null)RenderUtils.PrintErrLn(err);
            ascii = render.LoadTexture("Resources/ASCII-Extended.png");
            debugText = render.SpawnTextEntity(new EntityPosition(-Vector3.UnitX+Vector3.UnitY,Vector3.Zero,Vector3.One/30), "B", false, false, cameralessShader, ascii, true, null);

            
            grass = model.texture;

            camera = render.SpawnCamera(new Vector3(0, 0, 0), new Vector3(0, 0, 0), 90);

            GrassCubeBehavior = new RemoveOnTouchBehavior(cpuMesh);
            frames = 0;
            render.SetCamera(camera);

            render.OnUpdate += update; //subscribe the the update event
            render.OnRender += Render;

            render.Run();
        }
        static void update(double d){
            time += d;
            debugText.Text = "Entities: " + render.EntityCount() + "\n"
             + "Camera Position: " + camera.Position + '\n'
             + "Camera Rotation: " + camera.Rotation + '\n'
             + "FPS: " + (int)(frames/d);
            frames = 0;

            //sillyText.RotationY += (float)(Math.Sin(time*2/10)*Math.Sin(time*3/10))/20;
            KeyboardState keyboard = render.Keyboard();
            MouseState mouse = render.Mouse();
            //between -1 and 1
            if(keyboard.IsKeyDown(Keys.F)){
                EntityPosition pos = new EntityPosition(
                    camera.Position - Vector3.UnitY,
                    Vector3.Zero,
                    Vector3.One
                );
                render.SpawnEntity(pos, shader, model.mesh, model.texture, true, GrassCubeBehavior); 
            }
            
            if (keyboard.IsKeyReleased(Keys.C))
            {
                render.CursorLocked  = !render.CursorLocked;
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
            if(keyboard.IsKeyDown(Keys.LeftShift)) cameraSpeed = 1f;

            camera.Move(cameraInc * cameraSpeed);

            // Update camera baseda on mouse
            float sensitivity = 0.5f;

            if (render.CursorLocked || mouse.IsButtonDown(MouseButton.Right)) {
                camera.Rotation += new Vector3((mouse.Y - mouse.PreviousY) * sensitivity, (mouse.X - mouse.PreviousX) * sensitivity, 0);
            }
        }

        static void Render(double d){
            frames++;
        }
    }
}
