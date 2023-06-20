
using System;
using BasicGUI;
using VRenderLib.Interface;

namespace Trilateral.Game.Screen;

public sealed class MainMenuScreen : IScreen
{
    CenterContainer root;
    ButtonElement startGameButton;

    public MainMenuScreen(BasicGUIPlane gui, IRenderTexture font)
    {
        root = new CenterContainer(gui.GetRoot());
        startGameButton = new ButtonElement(root);
        new TextElement(startGameButton, 0xFFFFFF, 40, "START GAME", font, gui.GetDisplay(), 0);
    }
    public IScreen? Update(TimeSpan delta, BasicGUIPlane gui)
    {
        if(startGameButton.isDown)
        {
            OnExit();
            return new WorldScreen(gui, Program.Game.MainFont, Program.Game.StaticProperties, Program.Game.Settings, Program.Game.BlockRegistry);
        }
        return this;
    }
    public void Draw(TimeSpan delta)
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