namespace Examples;

using BasicGUI.Core;
using BasicGUI;

using Render;

using OpenTK.Mathematics;
public class ExampleTable
{
    public static void Run()
    {
        RenderSettings settings = new RenderSettings(){
            Size = new Vector2i(800, 600),
            VSync = true,
            BackgroundColor = 0x000000FF,
            TargetFrameTime = 1/60f,
        };
        IRender render = RenderUtils.CreateIdealRenderOrDie(settings);

        IRenderShader? shader = render.LoadShader("gui", out var err);
        if(shader is null){
            throw new Exception("lol shader no work", err);
        }
        RenderFont font = new RenderFont(render.LoadTexture("ascii.png"), shader);

        //Create the thing that connects BasicGUI and Raylib together so they can talk to each other.
        IDisplay display = new RenderDisplay();
        //a BasicGUIPlane is the main class responsible for, well, BasicGUI. You could make a RootNode directly, but I advise against it.
        BasicGUIPlane plane = new BasicGUIPlane(800, 600, display);
        //First, we want to put a table on the left center.
        IContainerNode leftCenter = new LayoutContainer(plane.GetRoot(), VAllign.center, HAllign.left);
        //Add the table as well.
        TableContainer table = new TableContainer((container) => {return new ColorOutlineRectElement(container, 0x666666ff, null, null, 5);}, leftCenter, 2, 10);
        //Add the elements to the table
        int fontSize = 15;
        uint textColor = 0xffffffff;
        new TextElement(table, textColor,  fontSize, "Fruit ",         font, display);
        new TextElement(table, textColor,  fontSize, "Color ",         font, display);
        new TextElement(table, 0xffff00ff, fontSize, "Banana ",        font, display);
        new TextElement(table, 0xffff00ff, fontSize, "Yellow ",        font, display);
        new TextElement(table, 0xff0000ff, fontSize, "Apple ",         font, display);
        new TextElement(table, 0xff0000ff, fontSize, "Red ",           font, display);
        new TextElement(table, 0xff3366ff, fontSize, "Dragonfruit ",   font, display);
        new TextElement(table, 0xff3366ff, fontSize, "Red ",           font, display);
        new TextElement(table, 0xff33ccff,  fontSize, "Mango ",        font, display);
        new TextElement(table, 0xff33ccff,  fontSize, "Multicolored ", font, display);

        //And of course, the mysterious centered text.
        CenterContainer center = new CenterContainer(plane.GetRoot());
        new TextElement(center, 0xff3366ff, fontSize, "Mysteriously Centered Text", font, display);

    render.OnRender += (delta) => {frame(delta, render, display, plane);};
        render.Run();
    }

    private static void frame(double delta, IRender render, IDisplay display, BasicGUIPlane plane)
    {
        //the RenderDisplay talks to the Render for us.
        Vector2 size = render.WindowSize();
        plane.SetSize((int)size.X, (int)size.Y);
        plane.Iterate();
        plane.Draw();
    }
}