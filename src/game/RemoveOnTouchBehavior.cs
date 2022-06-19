namespace Voxelesque.Game;

using Render;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;
using libvmodel;
class RemoveOnTouchBehavior : IEntityBehavior{
    VMesh _mesh;
    public RemoveOnTouchBehavior(VMesh mesh){
        _mesh = mesh;
    }
    public void Update(double time, double delta, IRenderEntity entity, KeyboardState keyboard, MouseState mouse){
        if(mouse.IsButtonDown(MouseButton.Left)){
            Vector2 normalizedCursorPos = new Vector2(mouse.Position.X / IRender.CurrentRender.WindowSize().X, mouse.Position.Y / IRender.CurrentRender.WindowSize().Y) * 2 -Vector2.One;
            if(entity != null && RenderUtils.MeshCollides(_mesh, normalizedCursorPos, entity.GetTransform() * IRender.CurrentRender.GetCamera().GetTransform(), null)){
                IRender.CurrentRender.DeleteEntityDelayed(entity);
            }
        }
    }

    public void Attach(IRenderEntity entity){
        RenderUtils.PrintLn("Attached GrassCubeBehavior to " + entity);
    }

    public void Detach(IRenderEntity entity){
        RenderUtils.PrintLn("Detached GrassCubeBehavior to " + entity);
    }

}