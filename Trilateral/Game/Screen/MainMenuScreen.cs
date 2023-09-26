
using System;
using BasicGUI;
using Trilateral.Utility;
using VRenderLib.Interface;

namespace Trilateral.Game.Screen;

public sealed class MainMenuScreen : IScreen
{
    readonly CenterContainer root;
    readonly StackingContainer stack;
    readonly TableContainer table;
    readonly ButtonElement startGameButton;
    readonly TextBoxElement worldName;
    public MainMenuScreen(BasicGUIPlane gui, IRenderTexture font)
    {
        root = new CenterContainer(gui.GetRoot());
        stack = new StackingContainer(root, StackDirection.down, 10);
        table = new TableContainer(
            (container) => new ColorRectElement(container, 0x333333FF, null, null, 0),
            stack, 2, 5
        );
        _ = new TextElement(table, 0xFFFFFFFF, 20, "World Name:", font, gui.GetDisplay(), 0);
        worldName = new TextBoxElement(table, 20, 0xFFFFFFFF, font, gui.GetDisplay(), 0);
        var textBoxBackground = new ColorRectElement(worldName, 0x666666FF, 20, 20, 0)
        {
            MinHeight = 20,
            MinWidth = 20
        };
        stack.Width = 100;
        stack.Height = 100;
        var centerStart = new CenterContainer(stack);
        startGameButton = new ButtonElement(centerStart);
        _ = new TextElement(startGameButton, 0xFFFFFF, 20, "START GAME", font, gui.GetDisplay(), 0);
    }
    public IScreen? Update(TimeSpan delta, BasicGUIPlane gui)
    {
        if(startGameButton.isDown)
        {
            OnExit();
            string worldName = this.worldName.GetText();
            if(worldName != string.Empty)
            {
                string worldPath = StringVerifiers.ConvertPathToSecure(worldName);
                return new MainGameScreen(gui, worldPath, worldName);
            }
        }
        return this;
    }
    public void Draw(TimeSpan delta, IDrawCommandQueue drawCommandQueue)
    {
        //the GUI nodes are drawn separately
    }
    public void OnExit()
    {
        if(root.Parent is null)return;
        //remove the start game button
        root.Parent.RemoveChild(root);
    }
}