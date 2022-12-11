using BasicGUI.Core;
using Render;
using Render.Util;
using OpenTK.Mathematics;

//a Texture and Shader for rendering a font.
public class RenderFont
{
    public RenderFont(IRenderTexture fontTexture, IRenderShader fontShader)
    {
        texture = fontTexture;
        shader  = fontShader;
    }
    public IRenderTexture texture;
    public IRenderShader shader;
}

public sealed class RenderDisplay : IDisplay
{
    #nullable disable
    public static RenderFont defaultFont;
    #nullable enable

    private static Vector2 ConvertPixelsToRender(int x, int y, Vector2 size)
    {
        return new Vector2((2/size.X)+1, (-2/size.Y)+1);
    }
    public void DrawPixel(int x, int y, uint rgb, byte depth = 0)
    {
        IRender.CurrentRender.WritePixelDirect(rgb, x, y);
    }
    public void FillRect(int x0, int y0, int x1, int y1, uint rgb, byte depth = 0)
    {
        for(int xi=x0; xi<x1; xi++)
        {
            for(int yi=y0; yi<y1; yi++)
            {
                DrawPixel(xi, yi, rgb);
            }
        }
    }
    //These were copied and translated from Wikipedia, of all places: https://en.wikipedia.org/wiki/Bresenham's_line_algorithm
    // Stackoverflow doesn't have ALL the answers.
    public void DrawLine(int x1, int y1, int x2, int y2, uint rgb, byte depth = 0)
    {
        if(int.Abs(y2-y1) < int.Abs(x2-x1))
        {
            if(x1 > x2)
                DrawLineLow(x2, y2, x1, y1, rgb);
            else
                DrawLineLow(x1, y1, x2, y2, rgb);
        }
        else
        {
            if(y1 > y2)
                DrawLineHigh(x2, y2, x1, y1, rgb);
            else
                DrawLineHigh(x1, y1, x2, y2, rgb);
        }
    }

    private void DrawLineLow(int x0, int y0, int x1, int y1, uint rgb)
    {
        int dx = x1-x0;
        int dy = y1-y0;
        int yi = 1;
        if(dy < 0)
        {
            yi = -1;
            dy = -dy;
        }
        int D = (2*dy) - dx;
        int y = y0;

        for(int x=x0; x<x1; x++)
        {
            DrawPixel(x, y, rgb);
            if(D > 0)
            {
                y += yi;
                D += (2 * (dy - dx));
            }
            else
            {
                D += 2*dy;
            }
        }
    }

    private void DrawLineHigh(int x0, int y0, int x1, int y1, uint rgb)
    {
        int dx = x1-x0;
        int dy = y1-y0;
        int xi = 1;
        if(dy < 0)
        {
            xi = -1;
            dx = -dx;
        }
        int D = (2*dx) - dy;
        int x = x0;

        for(int y=y0; y<y1; y++)
        {
            DrawPixel(x, y, rgb);
            if(D > 0)
            {
                x += xi;
                D += (2 * (dx - dy));
            }
            else
            {
                D += 2*dx;
            }
        }
    }
    public void DrawVerticalLine(int x, int y1, int y2, uint rgb, byte depth = 0)
    {
        DrawLine(x, y1, x, y2, rgb, depth);
    }
    public void DrawHorizontalLine(int x1, int x2, int y, uint rgb, byte depth = 0)
    {
        DrawLine(x1, y, x2, y, rgb, depth);
    }
    public void DrawImage(object image, int x, int y, byte depth = 0)
    {
        RenderImage renderImage = (RenderImage)image;
        IRender.CurrentRender.DrawTextureDirect(renderImage, x, y, (int)renderImage.width, (int)renderImage.height, 0, 0, (int)renderImage.width, (int)renderImage.height);
    }
    //This method is no joke.
    public void DrawImage(object image, int x, int y, int width, int height, int srcx, int srcy, int srcwidth, int srcheight, byte depth = 0)
    {
        RenderImage renderImage = (RenderImage)image;
        IRender.CurrentRender.DrawTextureDirect(renderImage, x, y, width, height, srcx, srcy, srcwidth, srcheight);
    }
    //Draw using a default font
    public void DrawText(int fontSize, string text, NodeBounds bounds, uint rgba)
    {
        IRender render = IRender.CurrentRender;
        Vector2 size = render.WindowSize();
        Vector3 textScales = new Vector3(1/size.X, -1/size.Y, 0);
        Vector3 pos = new Vector3(ConvertPixelsToRender(bounds.X ?? 0, bounds.Y ?? 0, size));
        EntityPosition entityPos = new EntityPosition(pos, Vector3.Zero, textScales);
        //TODO: don't completely regenerate and reupload the text mesh every frame
        var textMesh = MeshGenerators.BasicText(text, false, false, out var error);
        //THIS is how error handling should be done! Screw exceptions!
        if(textMesh is null)
        {
            System.Console.WriteLine(error);
            return;
        }
        var renderTextMesh = render.LoadMesh(textMesh.Value);
        render.RenderMeshDirect(entityPos, defaultFont.shader, renderTextMesh, defaultFont.texture, false);
    }
    //set the rendered size of a text element using the default font.
    public void TextBounds(int fontSize, string text, out int width, out int height)
    {
        //For the time being, Voxelesque's text rendering is extremely simplistic.
        // The rendered size of text is fairly simple to compute.
        int length = text.Length;
        IRender render = IRender.CurrentRender;
        Vector2 size = render.WindowSize();
        Vector3 textScales = new Vector3(1/size.X, -1/size.Y, 0);
        
    }
    public void DrawText(object font, int fontSize, string text, NodeBounds bounds, uint rgba);
    public void TextBounds(object font, int fontSize, string text, out int width, out int height);
    //INPUTS AND OUTPUTS
    public int GetMouseX();
    public int GetMouseY();
    public string GetClipboard();
    public void SetClipboard(string clip);
    public bool KeyDown(KeyCode key);
    public IEnumerable<KeyCode> DownKeys();
    public bool keyPressed(KeyCode key);
    public IEnumerable<KeyCode> PressedKeys();
    public bool keyReleased(KeyCode key);
    public IEnumerable<KeyCode> ReleasedKeys();
    public bool LeftMouseDown();
    public bool LeftMousePressed();
    public bool LeftMouseReleased();
    public bool CapsLock();
    public bool NumLock();
    public bool ScrollLock();
}