/*

VModel editor has the following structure:

root
|-LayoutContainer(topLeft)
| |-StackingContainer(right)
| | |-Button
| | | |-"File"
|-LayoutContainer(topRight)
|-StackingContainer(down)
| |-StackingContainer(right)
| | |-Button
| | | |-"vertices"
| | |-Button
| | | |-"triangles"
| |-TableContainer(depends lol)
*/

namespace VModelEditor;

using Render;
public static class Program
{
    public static void Main()
    {
        IRender render = RenderUtils.CreateIdealRenderOrDie(new RenderSettings());
        render.Run();
    }
}