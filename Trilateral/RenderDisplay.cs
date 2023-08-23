using VRenderLib;
using VRenderLib.Interface;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Linq;
using System.Collections.Generic;
using BasicGUI;
using vmodel;
using Trilateral.Utility;
using VRenderLib.Threading;
using VRenderLib.Utility;

namespace Trilateral;

//TODO: rewrite this entire class because it's honestly kinda messy and is missing a ton of features
// Preferrably rewrite it after actual font support is added

/**
<summary>
This class was originally created simply for getting BasicGUI to work.
However, it may be referenced and used for any rendering purpose.
It is frequently used for debug rendering.
</summary>
*/
public sealed class RenderDisplay : IDisplay
{
    public RenderDisplay(IRenderTexture defaultFontTexture)
    {
        this.defaultFont = defaultFontTexture;
        //position, textureCoords, color, blend between using the texture and using the color
        mesh = new MeshBuilder(new Attributes(new EAttribute[]{EAttribute.position, EAttribute.textureCoords, EAttribute.rgbaColor, EAttribute.scalar}));
        //TODO: use a system that allows the use of multiple shaders and stuff.
        shader = VRenderLib.VRender.Render.GetShader(
            //vertex shader code
            @"
            #version 330 core
            layout (location=0) in vec3 position;
            layout (location=1) in vec2 texCoords;
            layout (location=2) in vec4 rgba;
            layout (location=3) in float blend;
            //we don't apply any transform at all
            out vec4 fragColor;
            out vec2 texCoordsOut;
            out float fragBlend;
            void main()
            {
                fragBlend = blend;
                fragColor = rgba;
                texCoordsOut = texCoords;
                gl_Position = vec4(position, 1.0);
            }
            ",
            //fragment shader code - this is where some stuff happens
            @"
            #version 330 core
            out vec4 pixelOut;
            in vec4 fragColor;
            in vec2 texCoordsOut;
            in float fragBlend;
            uniform sampler2D tex;
            void main()
            {
                vec4 texColor = texture(tex, texCoordsOut);
                //blend between the two colors
                pixelOut = mix(texColor, fragColor, fragBlend);
                if(pixelOut.a < 0.9)discard; //discard transparent pixels
            }
            ",
            mesh.attributes
            );
    }
    //default font for text rendering
    public IRenderTexture defaultFont;
    private readonly MeshBuilder mesh;
    private readonly IRenderShader shader;
    public void BeginFrame()
    {
        mesh.Clear();
    }
    public void EndFrame()
    {
    }

    public void DrawToScreen(IDrawCommandQueue queue)
    {
        Profiler.PushRaw("GuiMesh");
        var vmesh = mesh.ToMesh();
        queue.Custom(() => {
            Profiler.PushRaw("GuiRender");
            var gpuMesh = VRender.Render.LoadMesh(vmesh);
            queue.DrawDirect(defaultFont, gpuMesh, shader, Enumerable.Empty<KeyValuePair<string, object>>(), false);
            Profiler.PopRaw("GuiRender");
        });
        Profiler.PopRaw("GuiMesh");
    }
    public void DrawPixel(int x, int y, uint rgb, byte depth = 0)
    {
        (var glX, var glY) = PixelToGL(x, y);
        (var glXp, var glYp) = PixelToGL(x+1, y+1);
        VRenderLib.VRender.ColorFromRGBA(out var r, out byte g, out byte b, out byte a, rgb);
        //We can get away with drawing a single triangle
        mesh.AddVertex(glX, glY, 0, 0, 0.5f, r/256f, g/256f, b/256f, a/256f, 1);
        mesh.AddVertex(glX, glYp, 0, 0, 0.5f, r/256f, g/256f, b/256f, a/256f, 1);
        mesh.AddVertex(glXp, glY, 0, 0, 0.5f, r/256f, g/256f, b/256f, a/256f, 1);
    }
    public void FillRect(int x0, int y0, int x1, int y1, uint rgb, byte depth = 0)
    {
        //Filling a rectangle is SUPER easy lol.
        (var glX0, var glY0) = PixelToGL(x0, y0);
        (var glX1, var glY1) = PixelToGL(x1, y1);
        VRenderLib.VRender.ColorFromRGBA(out var r, out byte g, out byte b, out byte a, rgb);

        //TODO: depth
        //pos(3), texcoord(2), color(4)
        //triangle one
        mesh.AddVertex(glX0, glY0, 0, 0, 0.5f, r/256f, g/256f, b/256f, a/256f, 1);
        mesh.AddVertex(glX1, glY1, 0, 0, 0.5f, r/256f, g/256f, b/256f, a/256f, 1);
        mesh.AddVertex(glX0, glY1, 0, 0, 0.5f, r/256f, g/256f, b/256f, a/256f, 1);
        //triangle two
        mesh.AddVertex(glX0, glY0, 0, 0, 0.5f, r/256f, g/256f, b/256f, a/256f, 1);
        mesh.AddVertex(glX1, glY0, 0, 0, 0.5f, r/256f, g/256f, b/256f, a/256f, 1);
        mesh.AddVertex(glX1, glY1, 0, 0, 0.5f, r/256f, g/256f, b/256f, a/256f, 1);
    }
    public void DrawLineWithThickness(int x1, int y1, int x2, int y2, uint rgb, float thickness, byte depth = 0)
    {
        _ = depth;
        var size = VRenderLib.VRender.Render.WindowSize();
        (var glX0, var glY0) = PixelToGL(x1, y1);
        (var glXf, var glYf) = PixelToGL(x2, y2);
        //TODO: find a way to do this without using trig functions (if that's even posible)
        (var sina, var cosa) = MathF.SinCos(MathF.Atan2(y1-y2, x1-x2));
        //This is so the line is always the same thickness no matter what the angle is
        var xfactor = (sina*thickness)/size.X;
        var yfactor = (cosa*thickness)/size.Y;
        glX0 -= xfactor/2;
        glY0 -= yfactor/2;
        glXf -= xfactor/2;
        glYf -= yfactor/2;
        var glX01 = glX0 + xfactor;
        var glY01 = glY0 + yfactor;
        var glXf1 = glXf + xfactor;
        var glYf1 = glYf + yfactor;
        //We need to convert the RGBA color into a vec4
        VRenderLib.VRender.ColorFromRGBA(out var r, out byte g, out byte b, out byte a, rgb);

        //We add the vertices to the batch thingy
        //TODO: depth
        //pos(3), texcoord(2), color(4)
        //triangle one
        mesh.AddVertex(glX0 , glY0 , 0, 0, 0.5f, r/256f, g/256f, b/256f, a/256f, 1);
        mesh.AddVertex(glXf , glYf , 0, 0, 0.5f, r/256f, g/256f, b/256f, a/256f, 1);
        mesh.AddVertex(glX01, glY01, 0, 0, 0.5f, r/256f, g/256f, b/256f, a/256f, 1);
        //triangle two
        mesh.AddVertex(glX01, glY01, 0, 0, 0.5f, r/256f, g/256f, b/256f, a/256f, 1);
        mesh.AddVertex(glXf1, glYf1, 0, 0, 0.5f, r/256f, g/256f, b/256f, a/256f, 1);
        mesh.AddVertex(glXf , glYf , 0, 0, 0.5f, r/256f, g/256f, b/256f, a/256f, 1);
    }

    public void DrawLine(int x1, int y1, int x2, int y2, uint rgb, byte depth = 0)
    {
        DrawLineWithThickness(x1, y1, x2, y2, rgb, 2, depth);
    }

    public void DrawVerticalLine(int x, int y1, int y2, uint rgb, byte depth = 0)
    {
        DrawLine(x, y1, x, y2, rgb, depth);
    }
    public void DrawHorizontalLine(int x1, int x2, int y, uint rgb, byte depth = 0)
    {
        DrawLine(x1, y, x2, y, rgb, depth);
    }

    //TODO: image drawing functions
    public void DrawImage(object image, int x, int y, byte depth = 0)
    {
    }
    //This method is no joke.
    public void DrawImage(object image, int x, int y, int width, int height, int srcx, int srcy, int srcwidth, int srcheight, byte depth = 0)
    {
    }
    //Draw using a default font
    public void DrawText(int fontSize, string text, NodeBounds bounds, uint rgba, byte depth)
    {
        //Draw text with default font.
        DrawText(defaultFont, fontSize, text, bounds, rgba, depth);
    }
    //set the rendered size of a text element using the default font.
    public void TextBounds(int fontSize, string text, out int width, out int height)
    {
        TextBounds(defaultFont, fontSize, text, out width, out height);
    }
    public void DrawText(object font, int fontSize, string text, NodeBounds bounds, uint rgba, byte depth)
    {
        if(font is not IRenderTexture)
        {
            //TODO: handle error more gracefully
            throw new Exception("Font is not a render texture!");
        }
        (var glx, var gly) = PixelToGL(bounds.X ?? 0, bounds.Y ?? 0);
        //This is why we need a custom shader - so that the text color and tint color can be blended nicely.
        VRenderLib.VRender.ColorFromRGBA(out var r, out byte g, out byte b, out byte a, rgba);
        //Don't worry about re-generating the mesh every time.
        // the mesh generator has a cache so it will reuse them if it can.
        var tmesh = VRenderLib.Utility.MeshGenerators.BasicText(text, false, false, out var err) ?? throw new Exception(err);
        uint attributes = tmesh.attributes.TotalAttributes();
        float[] vertices = tmesh.vertices;
        Vector2i screenSize = VRenderLib.VRender.Render.WindowSize();
        Vector2 scale = new Vector2(fontSize*2, fontSize*2)/screenSize;
        foreach(uint index in tmesh.indices)
        {
            //text mesh attributes are position, texCoord
            float xp = vertices[index*attributes];
            float yp = vertices[index*attributes+1];
            //float zp = vertices[index*attributes+2]
            float xt = vertices[index*attributes+3];
            float yt = vertices[index*attributes+4];
            //We need to transform this vertex into where it belongs
            //scale
            xp *= scale.X;
            yp *= scale.Y;
            //translation is easy
            xp += glx;
            yp += gly;
            //Now we add the whole thing.
            //TODO: depth
            //pos(3), texcoord(2), color(4)
            mesh.AddVertex(xp, yp, 0, xt, yt, r/256f, g/256f, b/256f, a/256f, 0);
        }
    }
    public void TextBounds(object font, int fontSize, string text, out int width, out int height)
    {
        //For the time being, Voxelesque's text rendering is extremely simplistic - every character is a square.
        // The rendered size of text in pixels is fairly simple to compute.
        // This only works for text elements that are one line.
        //width = text.Length*fontSize;
        //height = fontSize;
        width = 0;
        string[] lines = text.Split('\n');
        height = lines.Length*fontSize;
        foreach(string line in lines)
        {
            int lineWidth = line.Length * fontSize;
            if(lineWidth > width)width = lineWidth;
        }
    }

    //Extra rendering functions not used by BasicGUI
    public void DrawMeshLines(VMesh mesh, Matrix4 transform, uint RGBA, int thickness, out Exception? exception)
    {
try{
            //Check to make sure this mesh as a position component
            if(!mesh.attributes.Contains(EAttribute.position))
            {
                throw new Exception("Mesh does not contain a position attribute");
            }
            uint positionOffset = 0;
            foreach(EAttribute e in mesh.attributes)
            {
                if(e is EAttribute.position)
                {
                    break;
                }
                positionOffset += (uint)e %5;
            }
            //for each triangle
            for(uint triangleIndex = 0; triangleIndex < mesh.indices.Length/3; triangleIndex++)
            {
                //Get the vertices from the mesh
                uint vertexIndex = triangleIndex*3;
                uint v1i = mesh.indices[vertexIndex];
                uint v2i = mesh.indices[vertexIndex+1];
                uint v3i = mesh.indices[vertexIndex+2];
                Span<float> v1s = mesh.GetVertex(v1i);
                Span<float> v2s = mesh.GetVertex(v2i);
                Span<float> v3s = mesh.GetVertex(v3i);
                //extract the positions
                Vector3 v1 = new(v1s[(int)positionOffset], v1s[(int)positionOffset+1], v1s[(int)positionOffset+2]);
                Vector3 v2 = new(v2s[(int)positionOffset], v2s[(int)positionOffset+1], v2s[(int)positionOffset+2]);
                Vector3 v3 = new(v3s[(int)positionOffset], v3s[(int)positionOffset+1], v3s[(int)positionOffset+2]);
                //transform them by the matrix
                v1 = Vector3.TransformPerspective(v1, transform);
                v2 = Vector3.TransformPerspective(v2, transform);
                v3 = Vector3.TransformPerspective(v3, transform);

                if(v1.Z < 1.0f && v2.Z < 1.0f && v3.Z < 1.0f)
                {
                    (var v1pxx, var v1pxy) = GLToPixel(v1.X, v1.Y);
                    (var v2pxx, var v2pxy) = GLToPixel(v2.X, v2.Y);
                    (var v3pxx, var v3pxy) = GLToPixel(v3.X, v3.Y);
                    DrawLineWithThickness(v1pxx, v1pxy, v2pxx, v2pxy, RGBA, thickness);
                    DrawLineWithThickness(v2pxx, v2pxy, v3pxx, v3pxy, RGBA, thickness);
                    DrawLineWithThickness(v1pxx, v1pxy, v3pxx, v3pxy, RGBA, thickness);
                }
            }
            exception = null;
        } catch( Exception e)
        {
            exception = e;
        }
    }

    public void DrawMeshLines(VMesh mesh, Matrix4 transform, uint RGBA, out Exception? exception)
    {
        DrawMeshLines(mesh, transform, RGBA, 2, out exception);
    }
    //INPUTS AND OUTPUTS
    public int GetMouseX()
    {
        return (int)(IRender.CurrentRender.Mouse().X);
    }
    public int GetMouseY()
    {
        return (int)(IRender.CurrentRender.Mouse().Y);
    }
    public string GetClipboard()
    {
        return IRender.CurrentRender.GetClipboard();
    }
    public void SetClipboard(string clip)
    {
        IRender.CurrentRender.SetClipboard(clip);
    }
    public bool KeyDown(KeyCode key)
    {
        var keyboard = IRender.CurrentRender.Keyboard();
        //special cases for ctrl, shift, and alt.
        return key switch
        {
            KeyCode.shift => keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift),
            KeyCode.ctrl => keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl),
            KeyCode.alt => keyboard.IsKeyDown(Keys.LeftAlt) || keyboard.IsKeyDown(Keys.RightAlt),
            _ => keyboard.IsKeyDown(KeyCodeToKeys(key)),
        };
    }
    public IEnumerable<KeyCode> DownKeys()
    {
        foreach(KeyCode code in Enum.GetValues(typeof(KeyCode)))
        {
            if(KeyDown(code))yield return code;
        }
    }
    public bool KeyPressed(KeyCode key)
    {
        var keyboard = IRender.CurrentRender.Keyboard();
        //special cases for ctrl, shift, and alt.
        return key switch
        {
            KeyCode.shift => keyboard.IsKeyPressed(Keys.LeftShift) || keyboard.IsKeyPressed(Keys.RightShift),
            KeyCode.ctrl => keyboard.IsKeyPressed(Keys.LeftControl) || keyboard.IsKeyPressed(Keys.RightControl),
            KeyCode.alt => keyboard.IsKeyPressed(Keys.LeftAlt) || keyboard.IsKeyPressed(Keys.RightAlt),
            _ => keyboard.IsKeyPressed(KeyCodeToKeys(key)),
        };
    }
    public IEnumerable<KeyCode> PressedKeys()
    {
        foreach(KeyCode code in Enum.GetValues(typeof(KeyCode)))
        {
            if(KeyPressed(code))yield return code;
        }
    }
    public bool KeyReleased(KeyCode key)
    {
        var keyboard = IRender.CurrentRender.Keyboard();
        //special cases for ctrl, shift, and alt.
        return key switch
        {
            KeyCode.shift => keyboard.IsKeyReleased(Keys.LeftShift) || keyboard.IsKeyReleased(Keys.RightShift),
            KeyCode.ctrl => keyboard.IsKeyReleased(Keys.LeftControl) || keyboard.IsKeyReleased(Keys.RightControl),
            KeyCode.alt => keyboard.IsKeyReleased(Keys.LeftAlt) || keyboard.IsKeyReleased(Keys.RightAlt),
            _ => keyboard.IsKeyReleased(KeyCodeToKeys(key)),
        };
    }
    public IEnumerable<KeyCode> ReleasedKeys()
    {
        foreach(KeyCode code in Enum.GetValues(typeof(KeyCode)))
        {
            if(KeyReleased(code))yield return code;
        }
    }
    public bool LeftMouseDown()
    {
        return IRender.CurrentRender.Mouse().IsButtonDown(MouseButton.Left);
    }
    public bool LeftMousePressed()
    {
        var mouse = IRender.CurrentRender.Mouse();
        return !mouse.WasButtonDown(MouseButton.Left) && mouse.IsButtonDown(MouseButton.Left);
    }
    public bool LeftMouseReleased()
    {
        var mouse = IRender.CurrentRender.Mouse();
        return mouse.WasButtonDown(MouseButton.Left) && !mouse.IsButtonDown(MouseButton.Left);
    }
    public bool CapsLock()
    {
        return IRender.CurrentRender.Keyboard().IsKeyDown(Keys.CapsLock);
    }
    public bool NumLock()
    {
        return IRender.CurrentRender.Keyboard().IsKeyDown(Keys.NumLock);
    }
    public bool ScrollLock()
    {
        return IRender.CurrentRender.Keyboard().IsKeyDown(Keys.ScrollLock);
    }

    public float ScrollDelta()
    {
        return IRender.CurrentRender.Mouse().ScrollDelta.Y;
    }

    public static (float, float) PixelToGL(int x, int y)
    {
        Vector2 size = VRenderLib.VRender.Render.WindowSize();
        float glX = (x/(float)size.X - 0.5f) * 2;
        float glY = -(y/(float)size.Y - 0.5f) * 2;
        return (glX, glY);
    }

    public static (int, int) GLToPixel(float glx, float gly)
    {
        Vector2 size = VRenderLib.VRender.Render.WindowSize();
        int pxX = (int)((glx + 1)*(size.X/2f));
        int pxY = (int)((-gly + 1)*(size.Y/2f));
        return (pxX, pxY);
    }
    private static Keys KeyCodeToKeys(KeyCode key)
    {
        return key switch
        {
            KeyCode.backspace => Keys.Backspace,
            KeyCode.tab => Keys.Tab,
            KeyCode.enter => Keys.Enter,
            KeyCode.shift => Keys.LeftShift,
            KeyCode.ctrl => Keys.LeftControl,
            KeyCode.alt => Keys.LeftAlt,
            KeyCode.pauseBreak => Keys.Pause,
            KeyCode.caps => Keys.CapsLock,
            KeyCode.escape => Keys.Escape,
            KeyCode.space => Keys.Space,
            KeyCode.pageUp => Keys.PageUp,
            KeyCode.pageDown => Keys.PageDown,
            KeyCode.end => Keys.End,
            KeyCode.home => Keys.Home,
            KeyCode.left => Keys.Left,
            KeyCode.up => Keys.Up,
            KeyCode.right => Keys.Right,
            KeyCode.down => Keys.Down,
            KeyCode.printScreen => Keys.PrintScreen,
            KeyCode.insert => Keys.Insert,
            KeyCode.delete => Keys.Delete,
            KeyCode.zero => Keys.D0,
            KeyCode.one => Keys.D1,
            KeyCode.two => Keys.D2,
            KeyCode.three => Keys.D3,
            KeyCode.four => Keys.D4,
            KeyCode.five => Keys.D5,
            KeyCode.six => Keys.D6,
            KeyCode.seven => Keys.D7,
            KeyCode.eight => Keys.D8,
            KeyCode.nine => Keys.D9,
            KeyCode.a => Keys.A,
            KeyCode.b => Keys.B,
            KeyCode.c => Keys.C,
            KeyCode.d => Keys.D,
            KeyCode.e => Keys.E,
            KeyCode.f => Keys.F,
            KeyCode.g => Keys.G,
            KeyCode.h => Keys.H,
            KeyCode.i => Keys.I,
            KeyCode.j => Keys.J,
            KeyCode.k => Keys.K,
            KeyCode.l => Keys.L,
            KeyCode.m => Keys.M,
            KeyCode.n => Keys.N,
            KeyCode.o => Keys.O,
            KeyCode.p => Keys.P,
            KeyCode.q => Keys.Q,
            KeyCode.r => Keys.R,
            KeyCode.s => Keys.S,
            KeyCode.t => Keys.T,
            KeyCode.u => Keys.U,
            KeyCode.v => Keys.V,
            KeyCode.w => Keys.W,
            KeyCode.x => Keys.X,
            KeyCode.y => Keys.Y,
            KeyCode.z => Keys.Z,
            KeyCode.superLeft => Keys.LeftSuper,
            KeyCode.superRight => Keys.RightSuper,
            //case KeyCode.select: return Keys.se
            KeyCode.num0 => Keys.KeyPad0,
            KeyCode.num1 => Keys.KeyPad1,
            KeyCode.num2 => Keys.KeyPad2,
            KeyCode.num3 => Keys.KeyPad3,
            KeyCode.num4 => Keys.KeyPad4,
            KeyCode.num5 => Keys.KeyPad5,
            KeyCode.num6 => Keys.KeyPad6,
            KeyCode.num7 => Keys.KeyPad7,
            KeyCode.num8 => Keys.KeyPad8,
            KeyCode.num9 => Keys.KeyPad9,
            KeyCode.multiply => Keys.KeyPadMultiply,
            KeyCode.add => Keys.KeyPadAdd,
            KeyCode.subtract => Keys.KeyPadSubtract,
            KeyCode.decimalPoint => Keys.KeyPadDecimal,
            KeyCode.divide => Keys.KeyPadDivide,
            KeyCode.f1 => Keys.F1,
            KeyCode.f2 => Keys.F2,
            KeyCode.f3 => Keys.F3,
            KeyCode.f4 => Keys.F4,
            KeyCode.f5 => Keys.F5,
            KeyCode.f6 => Keys.F6,
            KeyCode.f7 => Keys.F7,
            KeyCode.f8 => Keys.F8,
            KeyCode.f9 => Keys.F9,
            KeyCode.f10 => Keys.F10,
            KeyCode.f11 => Keys.F11,
            KeyCode.f12 => Keys.F12,
            KeyCode.numLock => Keys.NumLock,
            KeyCode.scrollLock => Keys.ScrollLock,
            //case KeyCode.mute: return Keys.
            //case KeyCode.audioDown: return Keys.
            //case KeyCode.audioUp
            //case KeyCode.media: return Keys.
            //case KeyCode.app1: return Keys.
            //case KeyCode.app2
            KeyCode.semicolon => Keys.Semicolon,
            KeyCode.equals => Keys.Equal,
            KeyCode.comma => Keys.Comma,
            KeyCode.dash => Keys.Minus,
            KeyCode.period => Keys.Period,
            KeyCode.slash => Keys.Slash,
            KeyCode.grave => Keys.GraveAccent,
            KeyCode.bracketLeft => Keys.LeftBracket,
            KeyCode.backSlash => Keys.Backslash,
            KeyCode.bracketRight => Keys.RightBracket,
            KeyCode.quote => Keys.Apostrophe,
            _ => 0,//I would return Keys.Unknown, however OpenTK doesn't handle that case.
        };
    }
}