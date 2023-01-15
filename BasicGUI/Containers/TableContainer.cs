namespace BasicGUI;
using BasicGUI.Core;
using System;
using System.Collections.Generic;
//This class is a mess.
// Don't read this. At least look at the function declarations and not the actual implementation...
public sealed class TableContainer : IContainerNode
{
    public Func<TableContainer, INode> boxFactory;
    public int columns;
    public IContainerNode GetParent() => _parent;
    public void Interact(IDisplay display) 
    {
        foreach(INode node in _children)
        {
            node.Interact(display);
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

    public TableContainer(Func<TableContainer, INode> unboundedBoxfactory, int columns, List<INode> children, IContainerNode parent, int margin)
    {
        acceptChildren = true;
        _children = children;
        _parent = parent;
        parent.AddChild(this);
        boxFactory = unboundedBoxfactory;
        this.columns = columns;
        boundBoxes = new List<INode>();
        _margin = margin;
    }

    public TableContainer(Func<TableContainer, INode> unboundedBoxfactory, IContainerNode parent, int columns, int margin)
    :this(unboundedBoxfactory, columns, new List<INode>(), parent, margin) {}
    public void AddChild(INode child)
    {
        if(acceptChildren)
            _children.Add(child);
    }
    public void Iterate(){
        //Make sure we have enough bounding boxes to surround the elements.
        int numBoxes = boundBoxes.Count;
        List<INode> children = GetChildren();
        int numChildren = children.Count;
        if(numBoxes < children.Count)
        {
            acceptChildren = false;
            for(int i=numBoxes; i<numChildren; i++)
            {
                boundBoxes.Add(boxFactory(this));
            }
            acceptChildren = true;
        }
        //If the bounding boxes are containers, then we should also iterate those.
        foreach(INode node in boundBoxes)
        {
            if(node is IContainerNode container)
            {
                container.Iterate();
            }
        }

        foreach(INode child in _children){
            if(child is IContainerNode container){
                container.Iterate();
            }
        }
        PositionChildren();
        DetermineBounds();
    }
    private void PositionChildren()
    {
        List<INode> children = GetChildren();
        int numChildren = children.Count;
        //Determine the max sizes for the rows and columns. (the min size that fits all the elements is the max of their sizes)
        int[] columnMaxes = new int[columns];
        int[] rowMaxes = new int[numChildren / columns];
        for(int row = 0; row < rowMaxes.Length; row++)
        {
            for(int column = 0; column < columns; column++)
            {
                INode element = children[column + row*columns];
                int elementWidth = element.Width ?? 0; //null values are treated as 0
                int elementHeight = element.Height ?? 0;
                if(columnMaxes[column] < elementWidth)
                {
                    columnMaxes[column] = elementWidth;
                }
                if(rowMaxes[row] < elementHeight)
                {
                    rowMaxes[row] = elementHeight;
                }
            }
            rowMaxes[row] += _margin*2;
        }
        for(int column = 0; column < columns; column++)
        {
            columnMaxes[column] += _margin*2;
        }
        //Now that we know the sizes, we can position the elements. We place them on the top left of each cell for now.
        // TODO: allow elements placements to be more customizable
        int y = 0; //height is only run through once
        for(int row = 0; row < rowMaxes.Length; row++)
        {
            int x=0; //X is passed through for each row.
            for(int column = 0; column < columns; column++)
            {
                INode element = children[column + row*columns];
                element.XPos = x + _margin;
                element.YPos = y + _margin;
                element = boundBoxes[column + row*columns];
                element.XPos = x;
                element.YPos = y;
                element.Width = columnMaxes[column];
                element.Height = rowMaxes[row];

                x += columnMaxes[column];
            }
            y+= rowMaxes[row];
        }
    }
    public void Absolutize()
    {
        XPos = XPos ?? 0;//set null values to zero
        YPos = YPos ?? 0;
        this.XPos += _parent.XPos ?? 0;
        this.YPos += _parent.YPos ?? 0;
        foreach(INode child in _children){
            child.Absolutize();
        }
        foreach(INode node in boundBoxes)
        {
            node.Absolutize();
        }
    }

    //We also need to override the Draw method so the border elements can also be drawn.
    public void Draw(IDisplay display)
    {
        foreach(INode node in boundBoxes){
            node.Draw(display);
        }
        foreach(INode node in _children)
        {
            node.Draw(display);
        }
    }

    void DetermineBounds()
    {
        int top = int.MaxValue; //lowest Y value
        int left = int.MaxValue; // lowest X value
        int bottom = int.MinValue; //highest Y value
        int right = int.MinValue; //highest X value
        //Go through all of the children AND BOUND BOXES and find out our bounds.
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
        //Yep, I copied the code, no I don't care in the slightest.
        foreach(INode child in boundBoxes)
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

    


    public NodeBounds Bounds {set => _bounds = value; get => _bounds;}
    public int? XPos {set => _bounds.X = value; get => _bounds.X;}
    public int? YPos {set => _bounds.Y = value; get => _bounds.Y;}
    public int? Width {set => _bounds.W = value; get => _bounds.W;}
    public int? Height {set => _bounds.H = value; get => _bounds.H;}
    public List<INode> GetChildren() => _children;

    private int _margin;


    private IContainerNode _parent;
    private NodeBounds _bounds;


    private List<INode> _children;
    private List<INode> boundBoxes; //a chache of bounding elements. Each one serves as the background of the element with the same index.

    private bool acceptChildren; //weather or not to add children.

}