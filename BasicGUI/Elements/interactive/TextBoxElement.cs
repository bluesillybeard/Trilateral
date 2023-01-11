namespace BasicGUI;

using BasicGUI.Core;

using System.Collections.Generic;
using System.Text;
using System;

//This class represents a text box.
// It's a container since it needs to hold a TextElement and a background.
public sealed class TextBoxElement : IContainerNode
{
    public INode? back; //This is the element that actually gets drawn as the text background.
    private TextElement text; //the text object to render the text.

    public string GetText() {return text.Text;}
    public TextBoxElement(IContainerNode parent, int fontSize, uint fontColor, object font, IDisplay display)
    {
        back = null;
        _parent = parent;
        parent.AddChild(this);
        text = new TextElement(this, fontColor, fontSize, "", font, display);
    }
    public List<INode> GetChildren()
    {
        if(__childCache is null)
        {
            __childCache = new List<INode>();
            if(back is not null)__childCache.Add(back);
            __childCache.Add(text);
        }

        return __childCache ?? new List<INode>();
    }

    public void Iterate()
    {
        
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
        back = node;
    }
    public void Interact(IDisplay display)
    {
        //TODO
        bool caps = display.CapsLock();
        bool shift = display.KeyDown(KeyCode.shift);
        bool num = display.NumLock();
        StringBuilder builder = new StringBuilder();
        builder.Append(text.Text);
        foreach(KeyCode key in display.PressedKeys())
        {
            char? c = KeyConverter.KeyDown(key, caps, shift, num);
            if(c is not null)
            {
                //System.Console.Write(c);
                //System.Console.Write(' ');
                //System.Console.WriteLine((int)c);
                switch(c)
                {
                    case '\b':
                        if(builder.Length > 0)builder.Remove(builder.Length-1, 1);
                        break;
                    default:
                        builder.Append(c);
                        break;
                }
            }
        }
        text.Text = builder.ToString();

    }

    public void Draw(IDisplay display)
    {
        if(back is not null)back.Draw(display);
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
            if(back is not null)back.Bounds = value;
        } 
        get {
            if(back is not null)return back.Bounds;
            else return new NodeBounds(null, null, null, null);
        }
    }
    //Note: these tend to behave oddly when the drawable hasn't been set.
    public int? XPos {
        set {
            if(back is not null)back.XPos = value;
        }
        get {
            if(back is not null) return back.XPos;
            else return null;
        }
    }
    public int? YPos {
        set {
            if(back is not null)back.YPos = value;
        }
        get {
            if(back is not null) return back.YPos;
            else return null;
        }
    }
    public int? Width {
        set {
            if(back is not null)back.Width = value;
        }
        get {
            if(back is not null) return back.Width;
            else return null;
        }
    }
    public int? Height {
        set {
            if(back is not null)back.Height = value;
        }
        get {
            if(back is not null) return back.Height;
            else return null;
        }
    }
    private IContainerNode _parent;

    private List<INode>? __childCache;
}
