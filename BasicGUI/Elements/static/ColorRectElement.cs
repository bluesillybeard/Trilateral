namespace BasicGUI;

using BasicGUI.Core;
public sealed class ColorRectElement : AbstractElementNode
{
    public ColorRectElement(IContainerNode parent, uint rgba, int? width, int? height) : base(parent)
    {
        Width = width;
        Height = height;
        this.rgba = rgba;
    }
    public uint rgba;
    public override void Draw(IDisplay display)
    {
        int x = XPos ?? 0;
        int y = YPos ?? 0;
        int width = Width ?? 0;
        int height = Height ?? 0;
        display.FillRect(x, y, x + width, y + height, rgba);
    }
}