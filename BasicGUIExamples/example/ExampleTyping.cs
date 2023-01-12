namespace Examples;

using BasicGUI;
using BasicGUI.Core;

using Render;

using OpenTK.Mathematics;
public class ExampleTyping
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
        var texture = render.LoadTexture("ascii.png", out err);
        if(texture is null)throw new Exception("can't load ascii.png", err);
        RenderFont font = new RenderFont(texture, shader);
        //Create the thing that connects BasicGUI and Render together so they can talk to each other.
        IDisplay display = new RenderDisplay();

        BasicGUIPlane plane = new BasicGUIPlane(800, 600, display);
        CenterContainer container = new CenterContainer(plane.GetRoot());
        TextBoxElement textBox = new TextBoxElement(container, 20, 0xffffffff, font, display);

        //main loop.
        render.OnRender += (delta) => {frame(delta, render, display, plane);};
        render.OnUpdate += (delta) => {Update(delta, render, display, plane);};
        render.Run();
    }

    private static void frame(double delta, IRender render, IDisplay display, BasicGUIPlane plane)
    {
        //draw the stuff
        plane.Draw();
    }
    private static void Update(double delta, IRender render, IDisplay display, BasicGUIPlane plane)
    {
        //Update things.
        Vector2 size = render.WindowSize();
        plane.SetSize((int)size.X, (int)size.Y);
        plane.Iterate();
    }
    static void ButtonFrame(ButtonElement button)
    {
        ColorRectElement? rect = button.drawable as ColorRectElement;
        if(rect is not null) rect.rgba = 0xFFFFFFFF;
    }
    static void ButtonHover(ButtonElement button)
    {
        ColorRectElement? rect = button.drawable as ColorRectElement;
        if(rect is not null) rect.rgba = 0x666666FF;
    }
    static void ButtonClick(ButtonElement button)
    {
        ColorRectElement? rect = button.drawable as ColorRectElement;
        if(rect is not null) rect.rgba = 0xFF2222FF;
    }
}