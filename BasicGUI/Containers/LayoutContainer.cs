namespace BasicGUI;

using BasicGUI.Core;
using System.Collections.Generic;
/**
 * This is a container that anchors container children to a certain position relative to its parent.
 * Same remarks as with Centercontainer; so be careful who the parent is.
*/

public enum VAllign{
    top, center, bottom
}
public enum HAllign{
    left, center, right
}

public sealed class LayoutContainer : AbstractContainerNode
{
    public LayoutContainer(IContainerNode parent, List<INode> children, VAllign vertical, HAllign horizontal) : base(parent, children)
    {
        this.vertical = vertical;
        this.horizontal = horizontal;
    }
    public LayoutContainer(IContainerNode parent, VAllign vertical, HAllign horizontal) : this(parent, new List<INode>(), vertical, horizontal) {}
    public VAllign vertical;
    public HAllign horizontal;
    protected override void PositionChildren()
    {
        int? parentWidth = GetParent().Width;
        int? parentHeight = GetParent().Height;
        if(parentHeight is null || parentWidth is null)
        {
            System.Console.Error.WriteLine("ERROR: invalid parent bounds within LayoutContainer");
            return;
        }
        foreach(INode node in GetChildren())
        { 
            //A nodes position is relative to the top left.
            int? nodeWidth = node.Width;
            int? nodeHeight = node.Height;
            if(nodeHeight is null || nodeWidth is null)
            {
                System.Console.Error.WriteLine("Warning: invalid node bounds within LayoutContainer");
                continue;
            }
            switch(horizontal){
                case HAllign.left: node.XPos = 0;break;
                case HAllign.center: node.XPos = (parentWidth.Value - nodeWidth.Value)/2;break;
                case HAllign.right: node.XPos = parentWidth.Value - nodeWidth;break;
                default:node.XPos = null;break;
            }
            switch(vertical){
                case VAllign.top: node.YPos = 0;break;
                case VAllign.center: node.YPos = (parentHeight.Value - nodeHeight.Value)/2;break;
                case VAllign.bottom: node.YPos = parentHeight.Value - nodeHeight;break;
                default:node.YPos = null;break;
            }
        }
    }
}