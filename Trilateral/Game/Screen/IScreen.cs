using System;
using BasicGUI;
using VRenderLib.Interface;

namespace Trilateral.Game.Screen;

public interface IScreen
{
    IScreen? Update(TimeSpan delta, BasicGUIPlane gui);
    void Draw(TimeSpan delta, IDrawCommandQueue drawCommandQueue);
    void OnExit();
}