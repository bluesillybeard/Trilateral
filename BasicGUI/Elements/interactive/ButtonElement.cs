namespace BasicGUI;

using BasicGUI.Core;

using System.Collections.Generic;

using System;
//a button is the most basic element.
// You can push it, and... that's it really.
// The button does not have any interaction code itself, if you want to, for example, make it bigger when its hovered, you have to do that yourself.
// There are two ways to use a Button: boolean flags read each iteration, or function callbacks. You can use both at the same time if you would like.
public sealed class ButtonElement : IContainerNode
{
    public INode? drawable; //This is the element that actually gets drawn as the button.
    public bool hovered; //if this button is currently being hovered over.
    public bool clicked; //If this button is currently being held down
    public Action<ButtonElement>? hover;
    public Action<ButtonElement>? click;
    public Action<ButtonElement>? frame;

    public ButtonElement(IContainerNode parent, Action<ButtonElement>? hoverFrame, Action<ButtonElement>? clickFrame, Action<ButtonElement>? frame)
    {
        this.drawable = null;
        _parent = parent;
        parent.AddChild(this);
        click = clickFrame;
        hover = hoverFrame;
        this.frame = frame;
    }

    public ButtonElement(IContainerNode parent) : this(parent, null, null, null){}

    public List<INode> GetChildren()
    {
        if(__childCache is null && drawable is not null)
        {
            __childCache = new List<INode>();
            __childCache.Add(drawable);
        }
        return __childCache ?? new List<INode>();
    }

    public void Iterate()
    {
        if(frame is not null)frame(this);
    }

    public INode? GetSelectedNode()
    {
        return _parent.GetSelectedNode();
    }

    public void OnSelect(INode selection)
    {
        _parent.OnSelect(selection);
    }

    public void AddChild(INode node)
    {
        drawable = node;
    }
    public void Interact(IDisplay display)
    {
        hovered = false;
        clicked = false; //reset variables
        if(Bounds.ContainsPoint(display.GetMouseX(), display.GetMouseY()))
        {
            if(hover is not null)hover(this);
            hovered = true;
            if(display.LeftMouseDown()){
                clicked = true;
                if(click is not null)click(this);
            }
        }
    }

    public void Draw(IDisplay display)
    {
        if(drawable is not null)drawable.Draw(display);
    }
    public void Absolutize()
    {
        XPos = XPos ?? 0;//set null values to zero
        YPos = YPos ?? 0;
        this.XPos += _parent.XPos ?? 0;
        this.YPos += _parent.YPos ?? 0;
    }
    public IContainerNode GetParent()=> _parent;

    public NodeBounds Bounds {
        set {
            if(drawable is not null)drawable.Bounds = value;
        } 
        get {
            if(drawable is not null)return drawable.Bounds;
            else return new NodeBounds(null, null, null, null);
        }
    }
    //Note: these tend to behave oddly when the drawable hasn't been set.
    public int? XPos {
        set {
            if(drawable is not null)drawable.XPos = value;
        }
        get {
            if(drawable is not null) return drawable.XPos;
            else return null;
        }
    }
    public int? YPos {
        set {
            if(drawable is not null)drawable.YPos = value;
        }
        get {
            if(drawable is not null) return drawable.YPos;
            else return null;
        }
    }
    public int? Width {
        set {
            if(drawable is not null)drawable.Width = value;
        }
        get {
            if(drawable is not null) return drawable.Width;
            else return null;
        }
    }
    public int? Height {
        set {
            if(drawable is not null)drawable.Height = value;
        }
        get {
            if(drawable is not null) return drawable.Height;
            else return null;
        }
    }
    private IContainerNode _parent;

    private List<INode>? __childCache;
}
