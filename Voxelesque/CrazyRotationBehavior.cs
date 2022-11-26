namespace Voxelesque.Game;

using Render;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;
using vmodel;
class CrazyMovementBehavior : IEntityBehavior{
    private EntityPosition _vel;
    public CrazyMovementBehavior(EntityPosition vel){
        _vel = vel;
    }
    public void Update(double time, double delta, IRenderEntity entity, KeyboardState keyboard, MouseState mouse){
        entity.Position = entity.Position + _vel*(float)delta;
    }

    public void Attach(IRenderEntity entity){
    }

    public void Detach(IRenderEntity entity){
    }

}