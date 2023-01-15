namespace BasicGUI.Core;

using System.Collections.Generic;
using System.Numerics;
public class RootContainer : IContainerNode
{
    public RootContainer(int width, int height) : this(width, height, new List<INode>()){}
    public RootContainer(int width, int height, List<INode> children)
    {
        _children = children;
        Width = width;
        Height = height;
    }
    public IContainerNode? GetParent() => null;
    public void Iterate()
    {
        //Iterate children
        foreach(INode child in _children){
            child.XPos = null;
            child.YPos = null;
            if(child is IContainerNode container){
                container.Iterate();
            }
        }
    }
    public void Absolutize()
    {
        foreach(INode child in _children){
            child.Absolutize();
        }
    }
    public void Draw(IDisplay display)
    {
        foreach(INode node in _children){
            node.Draw(display);
        }
    }

    public void Interact(IDisplay display)
    {
        foreach(INode node in _children)
        {
            node.Interact(display);
        }
    }

    //We are the root container, so we keep track of who is selected.
    public INode? GetSelectedNode()
    {
        return _selection;
    }

    public void OnSelect(INode selection)
    {
        _selection = selection;
    }


    public NodeBounds Bounds {
        get => new NodeBounds(0, 0, Width, Height);
        set {
            //XPos and YPos must always be 0, so the position is ignored.
            Width = value.W;
            Height = value.H;
        }
    }
    public int? XPos {set {} get => 0;}
    public int? YPos {set {} get => 0;}
    public int? Width {set => _width = value ?? 0; get => _width;}
    public int? Height {set => _height = value ?? 0; get => _height;}
    public List<INode> GetChildren() => _children;
    public void AddChild(INode child) {_children.Add(child);}

    private List<INode> _children;
    private int _width;
    private int _height;

    private INode? _selection;

}