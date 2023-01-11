namespace BasicGUI;
using BasicGUI.Core;
public sealed class BasicGUIPlane
{
    public BasicGUIPlane(int width, int height, IDisplay display)
    {
        _root = new RootContainer(width, height);
        _display = display;
    }
    public void AddContainer(IContainerNode container)
    {
        _root.AddChild(container);
    }
    public void Iterate()
    {
        _root.Iterate();
        _root.Absolutize();
        _root.Interact(_display);
    }

    public void Draw()
    {
        _display.BeginFrame();
        _root.Draw(_display);
        _display.EndFrame();
    }

    public RootContainer GetRoot(){
        return _root;
    }

    public void SetSize(int width, int height)
    {
        _root.Width = width;
        _root.Height = height;
    }
    private RootContainer _root;
    private IDisplay _display;
}