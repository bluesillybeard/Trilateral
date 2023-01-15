namespace BasicGUI.Core;

using System.Collections.Generic;
public interface IContainerNode : INode
{
    List<INode> GetChildren();
    ///<summary>
    ///Does three things:
    /// -iterates child containers
    /// -positions its children
    /// -determines its bounds
    /// </summary>
    void Iterate();
    void AddChild(INode child);
    void OnSelect(INode selection);

    INode? GetSelectedNode();
}