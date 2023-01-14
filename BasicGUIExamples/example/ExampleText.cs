namespace Examples;
using BasicGUI;
using BasicGUI.Core;
using VRender;
using OpenTK.Mathematics;

public class ExampleText
{
    public static void Run()
    {
        RenderSettings settings = new RenderSettings()
        {
            Size = new Vector2i(800, 600),
            WindowTitle = "I am text.",
            BackgroundColor = 0x000000FF,
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

        CenterContainer container = new CenterContainer(plane.GetRoot());
        TextElement text = new TextElement(container, 0xFFFFFF, 20, "I am text. Aren't I great?", font, display);
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
}