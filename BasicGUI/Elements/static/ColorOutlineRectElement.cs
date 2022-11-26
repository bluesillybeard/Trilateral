namespace BasicGUI;

using BasicGUI.Core;
//This class is basically just a ColorRect element, except it draws four lines instead of filling it in.
public sealed class ColorOutlineRectElement : AbstractElementNode
{
    public ColorOutlineRectElement(IContainerNode parent, uint rgba, int? width, int? height, int thickness) : base(parent)
    {
        Width = width;
        Height = height;
        this.rgba = rgba;
        this.thickness = thickness;
    }
    public uint rgba;
    public int thickness;
    public override void Draw(IDisplay display)
    {
        int x = XPos ?? 0;
        int y = YPos ?? 0;
        int width = Width ?? 0;
        int height = Height ?? 0;
        int yf = y + height;
        int xf = x + width;
		for (int offset = 0; offset < thickness; offset++)
		{
			display.drawHorizontalLine(x + offset, xf - offset, y + offset, rgba);
			display.drawHorizontalLine(x + offset-1, xf - offset, yf - offset, rgba);
			display.drawVerticalLine  (x + offset, y + offset, yf - offset, rgba);
			display.drawVerticalLine  (xf - offset, y + offset, yf - offset, rgba);
		}
    }
}