namespace BasicGUI;

using BasicGUI.Core;
public class ColorBackgroundElement : AbstractElementNode
{
    public uint color;
    public ColorBackgroundElement(IContainerNode parent, uint color) : base(parent)
    {
        this.color = color;
    }
    public override void Interact(IDisplay display)
    {
        //Set size to zero so there isn't any effect the placement of other objects.
        this.Bounds = new NodeBounds(null, null, 0, 0);
        if(this.GetParent() is AbstractContainerNode parent) parent.shrink = false;
        base.Interact(display);
    }

    public override void Draw(IDisplay display)
    {
        IContainerNode parent = GetParent();
        display.FillRect(parent.XPos ?? 0, parent.YPos ?? 0, (parent.XPos ?? 0) + (parent.Width ?? 0), (parent.YPos ?? 0) + (parent.Height ?? 0), color);
    }
}