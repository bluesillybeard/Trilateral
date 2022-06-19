namespace VModelEdit;
using Render;
class Program{
    public static void Main(){
        IRender render = RenderUtils.CreateIdealRender();
        render.Init(new RenderSettings());

        render.Run();
    }
}