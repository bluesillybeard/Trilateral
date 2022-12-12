namespace Examples;

using BasicGUI.Core;
using BasicGUI;


public class ExampleTable
{
    public static void Main()
    {
        //we want the window to be resizable. obviously.
        ConfigFlags flags = ConfigFlags.FLAG_WINDOW_RESIZABLE;
        Raylib.SetConfigFlags(flags);
        Raylib.InitWindow(800, 600, "BasicGUI test using Raylib");
        //COMIC SANS FTW!!!!!!!!!!!
        Font font = Raylib.LoadFontEx("TSCu_Comic.ttf", 128, null, 0);

        //Create the thing that connects BasicGUI and Raylib together so they can talk to each other.
        IDisplay display = new RaylibDisplay();
        //a BasicGUIPlane is the main class responsible for, well, BasicGUI. You could make a RootNode directly, but I advise against it.
        BasicGUIPlane plane = new BasicGUIPlane(800, 600, display);
        //First, we want to put a table on the left center.
        IContainerNode leftCenter = new LayoutContainer(plane.GetRoot(), VAllign.center, HAllign.left);
        //Add the table as well.
        TableContainer table = new TableContainer((container) => {return new ColorOutlineRectElement(container, 0x666666ff, null, null, 5);}, leftCenter, 2, 10);
        //Add the elements to the table
        int fontSize = 25;
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
        new TextElement(center, 0xff3366ff, 25, "Mysteriously Centered Text", font, display);

        //main loop.
        while(!Raylib.WindowShouldClose())
        {
            Raylib.PollInputEvents();
            //Update the plane so it knows what size the window is.
            plane.SetSize(Raylib.GetRenderWidth(), Raylib.GetRenderHeight());
            //This function iterates through the elements and positions them.
            // It does two passes; once to determine relative positions, and another to convert them to absolute positions.
            plane.Iterate();
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.BLACK);
            plane.Draw();
            Raylib.EndDrawing();
            
        }
    }
}