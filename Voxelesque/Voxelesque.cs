namespace Voxelesque.Game;

using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

using VRender;

using System;

using vmodel;
public sealed class Voxelesque
{

    double time;
    IRender render;
    RenderEntityModel model;
    VMesh cpuMesh;
    IRenderShader shader;
    IRenderShader cameralessShader;
    IRenderTextEntity debugText;
    IRenderTexture grass;
    IRenderTexture ascii;
    Random random;

    RemoveOnTouchBehavior GrassCubeBehavior;

    RenderCamera camera;

    int frames;
    public Voxelesque()
    {
        System.Threading.Thread.CurrentThread.Name = "Main Thread";
        
        random = new Random((int)DateTime.Now.Ticks);
        RenderSettings settings = new RenderSettings();
        render = RenderUtils.CreateIdealRenderOrDie(settings);

        //initial loading stuff here - move to update method and make asynchronous when loading bar is added

        VModel? grassCubeModel = VModelUtils.LoadModel("Resources/vmf/models/GrassCube.vmf", out var errors);

        if(grassCubeModel is null)
        {
            string errories;
            if(errors is not null) errories = string.Join("/n", errors);
            else errories = "";
            throw new Exception("Error loading grass cube model:" + errories);
        }
        cpuMesh = grassCubeModel.Value.mesh;
        model = render.LoadModel(grassCubeModel.Value);
        IRenderShader? shader = render.LoadShader("Resources/Shaders/", out var e);
        if(shader is null)throw new Exception("Could not load main shader", e);
        this.shader = shader;
        IRenderShader? cameralessShader = render.LoadShader("Resources/Shaders/cameraless", out e);
        if(cameralessShader is null)throw new Exception("Could not load screenspace shader", e);
        this.cameralessShader = cameralessShader;
        IRenderTexture? ascii = render.LoadTexture("Resources/ASCII-Extended.png", out e);
        if(ascii is null)throw new Exception("Could noat load ascii texture", e);
        this.ascii = ascii;
        debugText = render.SpawnTextEntity(new EntityPosition(-Vector3.UnitX+Vector3.UnitY,Vector3.Zero,Vector3.One/30), "B", false, false, cameralessShader, ascii, true, null);

        
        grass = model.texture;

        camera = render.SpawnCamera(new Vector3(0, 0, 0), new Vector3(0, 0, 0), 90);

        GrassCubeBehavior = new RemoveOnTouchBehavior(cpuMesh);
        frames = 0;
        render.SetCamera(camera);

        render.OnUpdate += update; //subscribe the the update event
        render.OnRender += Render;
    }

    public void Run()
    {
        render.Run();
    }
    void update(double d){
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
        if(keyboard.IsKeyPressed(Keys.F)){
            EntityPosition vel = new EntityPosition(
                Vector3.Zero,
                new Vector3(random.NextSingle(), random.NextSingle(), random.NextSingle()) * 5,
                Vector3.Zero

            );
            EntityPosition pos = new EntityPosition(
                camera.Position - Vector3.UnitY,
                Vector3.Zero,
                new Vector3(random.NextSingle(), random.NextSingle(), random.NextSingle()) * 5
            );
            render.SpawnEntity(pos, shader, model.mesh, model.texture, true, new CrazyMovementBehavior(vel)); 
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

    void Render(double d){
        frames++;
    }
}