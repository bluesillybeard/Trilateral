namespace BasicGUI;

using BasicGUI.Core;
public sealed class TextElement : AbstractElementNode
{
    public TextElement(IContainerNode parent, uint rgba, int fontSize, string text, object font, IDisplay display) : base(parent)
    {
        this.rgba = rgba;
        this.fontSize = fontSize;
        this.display = display;
        _text = text;
        this.font = font;
        SetText(text);
    }
    public uint rgba;
    public int fontSize;
    public object font;
    private string _text;
    public string Text {get => _text; set => SetText(value);}
    public IDisplay display;
    public void SetText(string text)
    {
        _text = text;
        display.TextBounds(font, fontSize, _text, out int width, out int height);
        Width = width;
        Height = height;
    }
    public override void Draw(IDisplay display)
    {
        display.DrawText(font, fontSize, _text, Bounds, rgba);
    }
}