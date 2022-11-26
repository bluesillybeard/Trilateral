//This container works by stacking elements one after the other in a certain direction.
namespace BasicGUI;

using BasicGUI.Core;

//Similar to Godot's HBoxContainer or VBoxContainer, except the direction is a variable rather than a completely separate class.
public enum StackDirection
{
    up, left, down, right
}
public sealed class StackingContainer : AbstractContainerNode
{
    public StackDirection direction;
    public int separation;
    public StackingContainer(IContainerNode parent, StackDirection direction, int separation = 0) : base(parent)
    {
        this.direction = direction;
        this.separation = separation;
    }

    protected override void PositionChildren()
{
        int x=0;
        int y=0;
        //In case you didn't know, I STILL believe Java's switch statments are better than C#'s
        // Adding something similar to Rust's Match statement would be great, and would fill that void of Java's superior switch syntax
        switch(direction)
        {
            case StackDirection.up:
            {
                foreach(INode node in GetChildren())
                {
                    node.YPos = y;
                    y -= separation + node.Height ?? 0;
                }
                break;
            }
            case StackDirection.down:
            {
                foreach(INode node in GetChildren())
                {
                    node.YPos = y;
                    y += separation + node.Height ?? 0;
                }
                break;
            }
            case StackDirection.left:
            {
                foreach(INode node in GetChildren())
                {
                    node.XPos = x;
                    x -= separation + node.Width ?? 0;
                }
                break;
            }
            case StackDirection.right:
            {
                foreach(INode node in GetChildren())
                {
                    node.XPos = x;
                    x += separation + node.Width ?? 0;
                }
                break;
            }
            //Don't you just love a good pile of brackets? xD
            {{{{{}}}}}{{}{{{{}}}}{{{}{{}}{{}{{{{}{}}}}{{}}}}{{{}}}}}{{{}}{{}{{}}}{{{{{}}{}{}{{{}{}{}{}{}}{}{{}}{}{}{}{}{}{{}}}}}}{{}}}
        }
    }
}