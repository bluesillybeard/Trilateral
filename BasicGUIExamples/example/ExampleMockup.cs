//This is simply a mockup. No interaction at all. Interaction won't be added until I have the rendering side of things mostly done.
///Mockup concept:


/*
____________________________________________________________________
File View Settings Transform               |Vertices Triangles     |
___________________________________________|ind|0-0|0-1|0-2|1-0|1-1|
                                           |0  |x  |y  |z  |x  |y  |
                                           |___|___|___|___|___|___|
                                           |1  |x  |y  |z  |x  |y  |
                                           |___|___|___|___|___|___|
                                           |2  |x  |y  |z  |x  |y  |
                                           |___|___|___|___|___|___|
                                           |3  |x  |y  |z  |x  |y  |
                                           |___|___|___|___|___|___|
                                           |4  |x  |y  |z  |x  |y  |
                                           |___|___|___|___|___|___|
                                           |5  |x  |y  |z  |x  |y  |
___________________________________________|___|___|___|___|___|___|

Supposed tree for that:

root
|
|
|-LayoutContainer(top left) - StackingContainer(right)|-"File"
|                                                     |-"View"
|                                                     |-"Settings"
|                                                     |-"Transform"
|
|-LayoutContainer(top right) - StackingContainer(down)|- StackingContainer(right)|-"Vertices"
|                                                     |                          |-"Triangles"
|                                                     |
|                                                     |- TableContainer(6x)|-"ind"
|                                                                          |-"0-0"
|                                                                          ...a bunch of elements that I don't want to put here lol
|
|-The bottom left thingy will be worried about later.
This is a mockup. In the actual app it would be used to display a 3D preview (like blender), but for now it will wait.

Now for the real code for that lol.
*/
namespace Examples;
using BasicGUI;
using BasicGUI.Core;
using Render;
using OpenTK.Mathematics;
public sealed class ExampleMockup
{
    const int fontSize = 15;
    public static void Run()
    {
        //initing stuff
        RenderSettings settings = new RenderSettings()
        {
            Size = new Vector2i(800, 600),
            WindowTitle = "a GUI mockup example",
            BackgroundColor = 0x000000FF,
            TargetFrameTime = 1/60f,
            VSync = false,
        };

        IRender render = RenderUtils.CreateIdealRenderOrDie(settings);
        IDisplay display = new RenderDisplay();
        BasicGUIPlane plane = new BasicGUIPlane(800, 600, display);
        //We also need to load a font, which requires a shader and a texture.
        var shader = render.LoadShader("gui", out var error);
        if(shader is null)throw new Exception("shader no do thing", error);
        var texture = render.LoadTexture("ascii.png", out error);
        if(texture is null)throw new Exception("can't load ascii.png", error);
        RenderFont font = new RenderFont(texture, shader);
        RenderDisplay.defaultFont = font;

        IContainerNode root = plane.GetRoot();
        //Now for the hard part, actually building it out lol.
        //Top menu
        {
            var topLeft = new LayoutContainer(root, VAllign.top, HAllign.left);
            var rightStack = new StackingContainer(topLeft, StackDirection.right, fontSize);
            new TextElement(rightStack, 0xffffffff, fontSize, "File", font, display);
            new TextElement(rightStack, 0xffffffff, fontSize, "View", font, display);
            new TextElement(rightStack, 0xffffffff, fontSize, "Settings", font, display);
            new TextElement(rightStack, 0xffffffff, fontSize, "Transform", font, display);
        }
        //Vertices/Triangles menu
        {
            var topRight = new LayoutContainer(root, VAllign.top, HAllign.right);
            var downStack = new StackingContainer(topRight, StackDirection.down);
            var buttons = new StackingContainer(downStack, StackDirection.right, fontSize);
            new TextElement(buttons, 0xffffffff, fontSize, "Vertices", font, display);
            new TextElement(buttons, 0xffffffff, fontSize, "Triangles", font, display);
            var table = new TableContainer((container) => {return new ColorOutlineRectElement(container, 0x666666ff, null, null, 3);}, downStack, 6, 5);
            //The table has a lot, so I put it into its own function.
            FillTable(table, font, display);
        }
        //Render handles the looperoni. Thankfully my past self included a nice little callback for just this kind of occasion.
        // I'm still deciding if using static variables or passing it like this is better.
        // obviously separating it to be entirely object based would be ideal in this case.
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

    private static void FillTable(TableContainer table, RenderFont font, IDisplay disp)
    {
        //top row. Hard-coded because im lazy lol
        new TextElement(table, 0xffffffff, fontSize, "ind", font, disp);
        new TextElement(table, 0xffffffff, fontSize, "0-0", font, disp);
        new TextElement(table, 0xffffffff, fontSize, "0-1", font, disp);
        new TextElement(table, 0xffffffff, fontSize, "0-2", font, disp);
        new TextElement(table, 0xffffffff, fontSize, "1-0", font, disp);
        new TextElement(table, 0xffffffff, fontSize, "1-1", font, disp);
        //The next rows are procedurally generation, kinda. Not really lol
        for(int i=0; i<50; i++)
        {
            new TextElement(table, 0xffffffff, fontSize, i.ToString(), font, disp);
            new TextElement(table, 0xffffffff, fontSize, "x", font, disp);
            new TextElement(table, 0xffffffff, fontSize, "y", font, disp);
            new TextElement(table, 0xffffffff, fontSize, "z", font, disp);
            new TextElement(table, 0xffffffff, fontSize, "x", font, disp);
            new TextElement(table, 0xffffffff, fontSize, "y", font, disp);
        }
    }
}