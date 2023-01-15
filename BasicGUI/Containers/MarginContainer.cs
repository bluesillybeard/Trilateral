namespace BasicGUI;

using BasicGUI.Core;

public sealed class MarginContainer : AbstractContainerNode
{

    public uint Left, Top, Right, Bottom;
    public MarginContainer(IContainerNode parent, uint left, uint top, uint right, uint bottom) : base(parent)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public MarginContainer(IContainerNode parent, uint margin) : this(parent, margin, margin, margin, margin) {}

    public override void Iterate()
    {
        //Before the children are placed,
        // we need to subtract the margin from our size,
        // so elements below are placed properly
        this.Bounds = GetParent().Bounds;
        this.XPos = null;
        this.YPos = null;
        this.Height -= (int)(Top + Bottom);
        this.Width -= (int)(Left + Right);

        base.Iterate();
        this.XPos = (int)Left;
        this.YPos = (int)Top;
    }

    protected override void PositionChildren()
    {
    }
}