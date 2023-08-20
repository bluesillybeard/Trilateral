
using System;
using BasicGUI;
using OpenTK.Graphics.ES11;
using OpenTK.Mathematics;
using VRenderLib;
using VRenderLib.Interface;

namespace Trilateral.Game.Screen;

public sealed class MainMenuScreen : IScreen
{
    CenterContainer root;
    StackingContainer stack;
    TableContainer table;
    ButtonElement startGameButton;
    TextBoxElement worldName;
    
    public MainMenuScreen(BasicGUIPlane gui, IRenderTexture font)
    {
        root = new CenterContainer(gui.GetRoot());
        stack = new StackingContainer(root, StackDirection.down, 10);
        table = new TableContainer(
            (container) => {return new ColorRectElement(container, 0x333333FF, null, null, 0);},
            stack, 2, 5
        );
        new TextElement(table, 0xFFFFFFFF, 20, "World Name:", font, gui.GetDisplay(), 0);
        worldName = new TextBoxElement(table, 20, 0xFFFFFFFF, font, gui.GetDisplay(), 0);
        var textBoxBackground = new ColorRectElement(worldName, 0x666666FF, 20, 20, 0);
        textBoxBackground.MinHeight = 20;
        textBoxBackground.MinWidth = 20;
        stack.Width = 100;
        stack.Height = 100;
        var centerStart = new CenterContainer(stack);
        startGameButton = new ButtonElement(centerStart);
        new TextElement(startGameButton, 0xFFFFFF, 20, "START GAME", font, gui.GetDisplay(), 0);
    }
    public IScreen? Update(TimeSpan delta, BasicGUIPlane gui)
    {
        Vector2 mousePos = VRender.Render.Mouse().Position;
        Vector2i size = VRender.Render.WindowSize();
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