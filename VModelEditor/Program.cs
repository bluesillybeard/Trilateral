namespace VModelEdit;
using Render;
using OpenTK.Mathematics;
class Program{
    //Yes, I'm using the engine designed for making a very certain type of game to create a GUI application.
    // What could possibly go wrong? (foreshadowing?)
    
    //This is an excruciatingly simple 3D model editor.
    //The first step is to load a 3d model file - for now it only supports vmf (I'll add gltf support later)
    //and you can edit individual vertex attributes and get a basic preview.
    //Every GUI element is fixed in place. Yeah, i'm pretty lazy.

    const float GuiScale = 0.045f;
    struct LoadedAssets{

        //TEXTURES AND MESHES:
        public IRenderTexture TextTexture; //The texture atlas of the ASCII character set.
        public IRenderTexture CurrentModelTexture; //The texture of the model we are currently editing.
        public IRenderMesh CurrentModelMesh; //the mesh we are currently editing.
        public IRenderMesh GUIMesh; //the mesh is created from code that generates the GUI mesh to render. The text is rendered separately.

        //SHADERS:

        public IRenderShader GUITextShader; //Shader for text rendering - doesn't apply camera transform
        public IRenderShader GUIShader; //shader for GUI - doesn't apply camera transform, and uses the surface normal as an RGB color.
        public IRenderShader CurrentModelshader; //shader for the model currently being edited/displayed.
    }
    static LoadedAssets assets;

    struct Entities{
        public IRenderEntity CurrentModelEntity; //The entity that represents the current model.
        //Its mesh is updated every time it is changed, using the IRenderMesh.ReData method.
        public IRenderTextEntity GUITopBarText; //the top bar of buttons
        public IRenderTextEntity GUISelector; //the text for selecting between vertices and triangles
        public IRenderTextEntity GUIVertices; //displays either the vertices or the triangles.
        public IRenderEntity GUIEntity; //the entity that contains the GUI's mesh data.

    }
    static Entities entities;
    public static void Main(){
        IRender render = RenderUtils.CreateIdealRender();
        render.Init(new RenderSettings());
        render.OnUpdate += Update;
        //Load the required assets and initialize the meshes.
        assets.TextTexture = render.LoadTexture("ascii.png");
        assets.GUIMesh = render.LoadMesh(new float[8*3], new uint[3]); //An initial mesh before its generated. One day I'll properly implement empty meshes.
        assets.CurrentModelshader = render.LoadShader("model");
        assets.GUIShader = render.LoadShader("gui");
        assets.GUITextShader = render.LoadShader("text");
        //create the required entities
        entities.GUIEntity = render.SpawnEntity(EntityPosition.Zero, assets.GUIShader, assets.GUIMesh, null, false, null);
        entities.GUITopBarText = render.SpawnTextEntity(new EntityPosition(new Vector3(-1f, 1f, 0), Vector3.Zero, Vector3.One*GuiScale), "File|View|Settings|Tools", false, false, assets.GUITextShader, assets.TextTexture, false, null);
        entities.GUIVertices = render.SpawnTextEntity(EntityPosition.Zero, 
@"PX  |PY  |PZ  |TX  |TY  |NX  |NY  |NZ  |
1234|5678|9012|3456|7890|1234|5678|9012|
1234|5678|9012|3456|7890|1234|5678|9012|
1234|5678|9012|3456|7890|1234|5678|9012|", false, false, assets.GUITextShader, assets.TextTexture, false, null);
        entities.GUISelector = render.SpawnTextEntity(EntityPosition.Zero, "vertices | triangles", false, false, assets.GUITextShader, assets.TextTexture, false, null);

        render.Run();
    }

    static void Update(double delta){
        //Update entities vertical scale based on the resolution of the window
        Vector2 viewSize = IRender.CurrentRender.WindowSize();
        float aspect = viewSize.X/viewSize.Y;
        entities.GUIVertices.ScaleY = entities.GUIVertices.ScaleX * aspect;
        entities.GUISelector.ScaleY = entities.GUISelector.ScaleX * aspect;
        entities.GUITopBarText.ScaleY = entities.GUITopBarText.ScaleX * aspect;

        //determine the position of the vertices/triangles selector text by putting it at the top right.
        entities.GUISelector.LocationY = 1;
        entities.GUISelector.LocationX = 1-(entities.GUISelector.Text.Length*GuiScale);
        entities.GUISelector.Scale = new Vector3(GuiScale, GuiScale*aspect, 0);
        //Place the vertices text on the bottom left of the top bar
        entities.GUIVertices.LocationY = 1-GuiScale*aspect;
        entities.GUIVertices.LocationX = -1+(entities.GUITopBarText.Text.Length*GuiScale);
        entities.GUIVertices.Scale = new Vector3(GuiScale*0.52f, GuiScale*0.52f*aspect, 0); //Don't you just love a gui that can't be reogranized without huge changes to the code? Me neither, but i'm too lazy to make it better.
        //If I want to change the GUI scale, I also have to change a whole bunch of other variariables because i'm totally good at making a GUI lol
        //But, for my first GUI from scratch, it's acceptable.
    }
}