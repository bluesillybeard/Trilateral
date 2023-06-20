using System;
using BasicGUI;

namespace Trilateral.Game.Screen;

public interface IScreen
{
    IScreen? Update(TimeSpan delta, BasicGUIPlane gui);
    void Draw(TimeSpan delta);
    void OnExit();
}