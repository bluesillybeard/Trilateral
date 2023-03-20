using System;

using VRender.Interface;
using VRender;
namespace Voxelesque.Game
{
    public static class Program
    {

        private static void Main()
        {
            System.Threading.Thread.CurrentThread.Name = "Main Thread";
            var random = new Random((int)DateTime.Now.Ticks);
            var settings = new RenderSettings(){
                TargetFrameTime = 1f/60f,
                BackgroundColor = 0x000000ff,
                WindowTitle = "Voxelesque",
                size = new OpenTK.Mathematics.Vector2i(800, 600),
            };
            VRenderLib.InitRender(settings);
            VRenderLib.Render.OnStart += Start;
            VRenderLib.Render.Run();
        }

        private static void Start()
        {
            Voxelesque game = new Voxelesque();
        }
    }
}
