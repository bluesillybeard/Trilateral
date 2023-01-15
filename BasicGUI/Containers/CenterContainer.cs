namespace BasicGUI;

using BasicGUI.Core;
using System.Numerics;
using System.Collections.Generic;
/**
 * This is a container that literally just centers all of its items.
 * Note that it depends on its parents bounds. This might cause strange behavior,
 * and it in immediate mode it will only work properly as a root.
 * I recomend that you only center objects relative to elements whose bounds are not determined by their children.
 * Example: the root since its bounds are always the window.
*/

public sealed class CenterContainer : AbstractContainerNode
{
    public CenterContainer(IContainerNode parent, List<INode> children) : base(parent, children){}
    public CenterContainer(IContainerNode parent) : base(parent){}
    protected override void PositionChildren()
    {
        int? parentWidth = GetParent().Width;
        int? parentHeight = GetParent().Height;
        if(parentHeight is null || parentWidth is null)
        {
            System.Console.Error.WriteLine("BIG ERROR: invalid parent bounds within CenterContainer");
            return;
        }
        foreach(INode node in GetChildren())
        { 
            //A nodes position is relative to the top left.
            int? nodeWidth = node.Width;
            int? nodeHeight = node.Height;
            if(nodeHeight is null || nodeWidth is null)
            {
                System.Console.Error.WriteLine("Warning: invalid node bounds within CenterContainer");
                continue;
            }
            node.XPos = (parentWidth.Value - nodeWidth.Value)/2;
            node.YPos = (parentHeight.Value - nodeHeight.Value)/2;
        }
    }

}