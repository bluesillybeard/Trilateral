namespace BasicGUI.Core;

public abstract class AbstractElementNode : IElementNode
{

    public AbstractElementNode(IContainerNode parent)
    {
        _parent = parent;
        parent.AddChild(this);
    }
    public virtual void Interact(IDisplay display) {}

    public abstract void Draw(IDisplay display);
    public void Absolutize()
    {
        XPos = XPos ?? 0;//set null values to zero
        YPos = YPos ?? 0;
        this.XPos += _parent.XPos ?? 0;
        this.YPos += _parent.YPos ?? 0;
    }
    public IContainerNode GetParent()=> _parent;

    public NodeBounds Bounds {set => _bounds = value; get => _bounds;}
    public int? XPos {set => _bounds.X = value; get => _bounds.X;}
    public int? YPos {set => _bounds.Y = value; get => _bounds.Y;}
    public int? Width {set => _bounds.W = value; get => _bounds.W;}
    public int? Height {set => _bounds.H = value; get => _bounds.H;}

    private IContainerNode _parent;
    private NodeBounds _bounds;

    

}