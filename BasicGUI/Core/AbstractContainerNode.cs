namespace BasicGUI.Core;

using System.Collections.Generic;
using System.Numerics;
public abstract class AbstractContainerNode : IContainerNode
{
    public AbstractContainerNode(IContainerNode parent) : this(parent, new List<INode>()){}
    public AbstractContainerNode(IContainerNode parent, bool shrink) : this(parent, new List<INode>(), shrink){}
    public AbstractContainerNode(IContainerNode parent, List<INode> children, bool shrink = true)
    {
        _children = children;
        _parent = parent;
        this.shrink = shrink;
        parent.AddChild(this);
    }
    public bool shrink; //weather or not to shrink the bounds of this container to the elements it contains.
    //IMPORTANT: Custom nodes that use this class as a base MUST implement this function. No exceptions.
    protected abstract void PositionChildren();
    public IContainerNode GetParent() => _parent;

    void DetermineBounds()
    {
        int top = int.MaxValue; //lowest Y value
        int left = int.MaxValue; // lowest X value
        int bottom = int.MinValue; //highest Y value
        int right = int.MinValue; //highest X value
        //Go through all of the children and find out our bounds.
        foreach(INode child in _children)
        {
            int childX = child.XPos ?? 0;
            int childY = child.YPos ?? 0;
            int childWidth = child.Width ?? 0;
            int childHeight = child.Height ?? 0;
            //actually get the mins and maxs
            if(top > childY)top = childY;
            if(left > childX)left = childX;
            int childBottom = childY+childHeight;
            if(bottom < childBottom)bottom = childBottom;
            int childRight = childX + childWidth;
            if(right < childRight)right = childRight;
        }
        _bounds.W = right - left;
        _bounds.H = bottom - top;
        //We don't find the position here, since the parent container is what determines the position.
        // We couldn't do that anyway since the position of an element is relative to its parent.
    }

    public virtual void Iterate()
    {
        //Iterate children
        foreach(INode child in _children){
            child.XPos = null;
            child.YPos = null;
            if(child is IContainerNode container){
                container.Iterate();
            }
        }
        PositionChildren(); //This is implemented by subclasses.
        if(shrink)DetermineBounds();
    }

    public virtual void Absolutize()
    {
        XPos = XPos ?? 0;//set null values to zero
        YPos = YPos ?? 0;
        this.XPos += _parent.XPos ?? 0;
        this.YPos += _parent.YPos ?? 0;
        foreach(INode child in _children){
            child.Absolutize();
        }
    }

    public virtual void Interact(IDisplay display)
    {
        foreach(INode node in _children)
        {
            node.Interact(display);
        }
    }

    public virtual void Draw(IDisplay display)
    {
        foreach(INode node in _children)
        {
            node.Draw(display);
        }
    }

    public INode? GetSelectedNode()
    {
        return _parent.GetSelectedNode();
    }

    public void OnSelect(INode selection)
    {
        _parent.OnSelect(selection);
    }

    public NodeBounds Bounds {set => _bounds = value; get => _bounds;}
    public int? XPos {set => _bounds.X = value; get => _bounds.X;}
    public int? YPos {set => _bounds.Y = value; get => _bounds.Y;}
    public int? Width {set => _bounds.W = value; get => _bounds.W;}
    public int? Height {set => _bounds.H = value; get => _bounds.H;}
    public List<INode> GetChildren() => _children;
    public void AddChild(INode child) {_children.Add(child);}


    private IContainerNode _parent;
    private NodeBounds _bounds;


    private List<INode> _children;

}