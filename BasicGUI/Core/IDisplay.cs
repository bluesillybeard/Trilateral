namespace BasicGUI.Core;

using System.Collections.Generic;
//You must implement this class in order to integrate this into your project.
public interface IDisplay
{
    public static void ColorFromUint(out byte r, out byte g, out byte b, out byte a, uint rgba)
    {
        r = (byte)((rgba>>24)&0xFF);
        g = (byte)((rgba>>16)&0xFF);
        b = (byte)((rgba>>8)&0xFF);
        a = (byte)(rgba&0xFF);
    }
    public static uint UintFromColor(byte r, byte g, byte b, byte a)
    {
        uint rgba = 0;
        rgba |= (uint)r;
        rgba |= (uint)(g>>8);
        rgba |= (uint)(b>>16);
        rgba |= (uint)(a>>24);
        return rgba;
    }
    //DRAW METHODS
    void DrawPixel(int x, int y, uint rgb, byte depth = 0);
    void FillRect(int x0, int y0, int x1, int y1, uint rgb, byte depth = 0);
    void DrawLine(int x1, int y1, int x2, int y2, uint rgb, byte depth = 0);
    void DrawVerticalLine(int x, int y1, int y2, uint rgb, byte depth = 0);
    void DrawHorizontalLine(int x1, int x2, int y, uint rgb, byte depth = 0);
    void DrawImage(object image, int x, int y, byte depth = 0);
    //This method is no joke.
    void DrawImage(object image, int x, int y, int width, int height, int srcx, int srcy, int srcwidth, int srcheight, byte depth = 0);
    //Draw using a default font
    void DrawText(int fontSize, string text, NodeBounds bounds, uint rgba);
    //set the rendered size of a text element using the default font.
    void TextBounds(int fontSize, string text, out int width, out int height);
    void DrawText(object font, int fontSize, string text, NodeBounds bounds, uint rgba);
    void TextBounds(object font, int fontSize, string text, out int width, out int height);
    //INPUTS AND OUTPUTS
    int GetMouseX();
    int GetMouseY();
    string GetClipboard();
    void SetClipboard(string clip);
    bool KeyDown(KeyCode key);
    IEnumerable<KeyCode> DownKeys();
    bool keyPressed(KeyCode key);
    IEnumerable<KeyCode> PressedKeys();
    bool KeyReleased(KeyCode key);
    IEnumerable<KeyCode> ReleasedKeys();
    bool LeftMouseDown();
    bool LeftMousePressed();
    bool LeftMouseReleased();
    bool CapsLock();
    bool NumLock();
    bool ScrollLock();

    
}