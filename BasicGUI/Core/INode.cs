namespace BasicGUI.Core;

using System.Numerics;
public interface INode
{
    //The Root containers don't have parents, so there is a possibility if this being null.
    IContainerNode? GetParent();
    //These return null if it's unset, a value if it is set.
    NodeBounds Bounds {get;set;}
    int? XPos {get;set;}
    int? YPos {get;set;}
    int? Width {get;set;}
    int? Height {get;set;}

    //Elements just draw themself
    // Containers should call this function on all of its children.
    void Draw(IDisplay display);

    ///<summary> changes all of the relative coordinates of elements into absolute coordinates. </summary>
    void Absolutize();
    void Interact(IDisplay display);

}