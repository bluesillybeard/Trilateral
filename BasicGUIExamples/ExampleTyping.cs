// namespace RaylibExample;

// using BasicGUI;

// public class ExampleTyping
// {
//     public static void Main()
//     {
//         ConfigFlags flags = ConfigFlags.FLAG_WINDOW_RESIZABLE;
//         Raylib.SetConfigFlags(flags);
//         Raylib.InitWindow(800, 600, "BasicGUI test using Raylib");
//         Font font = Raylib.GetFontDefault();

//         RaylibDisplay display = new RaylibDisplay();
//         BasicGUIPlane plane = new BasicGUIPlane(800, 600, display);

//         CenterContainer container = new CenterContainer(plane.GetRoot());
//         TextBoxElement textBox = new TextBoxElement(container, 40, 0xffffffff, Raylib.GetFontDefault(), display);

//         //main loop.
//         while(!Raylib.WindowShouldClose())
//         {
//             plane.SetSize(Raylib.GetRenderWidth(), Raylib.GetRenderHeight());
//             //This function iterates through the elements and positions them.
//             // It does two passes; once to determine relative positions, and another to convert them to absolute positions.
//             plane.Iterate();
//             Raylib.BeginDrawing();
//             Raylib.ClearBackground(Color.BLACK);
//             plane.Draw();
//             Raylib.EndDrawing();
//         }
//     }
//     static void ButtonFrame(ButtonElement button)
//     {
//         ColorRectElement? rect = button.drawable as ColorRectElement;
//         if(rect is not null) rect.rgba = 0xFFFFFFFF;
//     }
//     static void ButtonHover(ButtonElement button)
//     {
//         ColorRectElement? rect = button.drawable as ColorRectElement;
//         if(rect is not null) rect.rgba = 0x666666FF;
//     }
//     static void ButtonClick(ButtonElement button)
//     {
//         ColorRectElement? rect = button.drawable as ColorRectElement;
//         if(rect is not null) rect.rgba = 0xFF2222FF;
//     }
// }