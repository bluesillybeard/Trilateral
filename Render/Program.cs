using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Render;

namespace Voxelesque
{
    public static class Program
    {
        private static void Main()
        {
            var nativeWindowSettings = new NativeWindowSettings()
            {
                Size = new Vector2i(800, 600),
                Title = "LearnOpenTK - Camera",
                // This is needed to run on macos
                Flags = ContextFlags.ForwardCompatible,
                APIVersion = new System.Version(3, 3), //using OpenGL 3.3
            };

            using (Window window = new Window(GameWindowSettings.Default, nativeWindowSettings))
            {
                window.Run();
            }
        }
    }
}
