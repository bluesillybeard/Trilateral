using System;
using System.Numerics;
using BasicGUI;
using BepuPhysics;
using BepuPhysics.Collidables;
using VRenderLib.Interface;

namespace Trilateral.Game.Screen;

public class PhysicsTestScreen : IScreen
{
    public PhysicsTestScreen()
    {
        //this box is supposed to fall
        var sim = Program.Game.physics.Sim;
        var boxShape = new Box(5, 5, 5);
        var boxBodyDesc = BodyDescription.CreateDynamic(RigidPose.Identity, boxShape.ComputeInertia(1), sim.Shapes.Add(boxShape), 1e-2f);
        sim.Bodies.Add(boxBodyDesc);
        //this is the floor
        sim.Statics.Add(new StaticDescription(new Vector3(0, -15f, 0), sim.Shapes.Add(new Box(2500, 30, 2500))));
    }
    public void Draw(TimeSpan delta, IDrawCommandQueue drawCommandQueue)
    {
        //YEETis mcGeEtueS.
        // The demos extract the shapes from the physics simulation itself.
        // I find that to be quite frankly disgusting, but it is what it is.
        // (It seriously took be FOREVER to figure out what was even going on)
        // (just so I could figure out how I could draw things myself.)
        
    }
    public IScreen? Update(TimeSpan delta, BasicGUIPlane gui)
    {
        return this;
    }
    public void OnExit()
    {

    }
}