using BasicGUI;
using BasicGUI.Core;
using Render;

public sealed class RenderDisplay : IDisplay
{
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
/*
plotLine(x0, y0, x1, y1)
    if abs(y1 - y0) < abs(x1 - x0)
        if x0 > x1
            plotLineLow(x1, y1, x0, y0)
        else
            plotLineLow(x0, y0, x1, y1)
        end if
    else
        if y0 > y1
            plotLineHigh(x1, y1, x0, y0)
        else
            plotLineHigh(x0, y0, x1, y1)
        end if
    end if
*/
    }

    private void DrawLineLow(int x0, int y0, int x1, int y1)
    {
        //determine the dx and dy of the dy/dx slope. This is the algrebra d (delta _), not the calculus d (infinitely small change in _).
        int dx = x1-x0;
        int dy = y1-y0;
    /*
    yi = 1
    if dy < 0
        yi = -1
        dy = -dy
    end if
    D = (2 * dy) - dx
    y = y0

    for x from x0 to x1
        plot(x, y)
        if D > 0
            y = y + yi
            D = D + (2 * (dy - dx))
        else
            D = D + 2*dy
        end if
    */
    }

    private void DrawLineHigh()
    {
        /*
            dx = x1 - x0
    dy = y1 - y0
    xi = 1
    if dx < 0
        xi = -1
        dx = -dx
    end if
    D = (2 * dx) - dy
    x = x0

    for y from y0 to y1
        plot(x, y)
        if D > 0
            x = x + xi
            D = D + (2 * (dx - dy))
        else
            D = D + 2*dx
        end if
        */
    }
    public void DrawVerticalLine(int x, int y1, int y2, uint rgb, byte depth = 0);
    public void DrawHorizontalLine(int x1, int x2, int y, uint rgb, byte depth = 0);
    public void DrawImage(object image, int x, int y, byte depth = 0);
    //This method is no joke.
    public void DrawImage(object image, int x, int y, int width, int height, int srcx, int srcy, int srcwidth, int srcheight, byte depth = 0);
    //Draw using a default font
    public void DrawText(int fontSize, string text, NodeBounds bounds, uint rgba);
    //set the rendered size of a text element using the default font.
    public void TextBounds(int fontSize, string text, out int width, out int height);
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