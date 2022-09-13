namespace Render;

using OpenTK.Mathematics;
using OpenTK.Windowing;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

using vmodel;

using OpenTK;

using ImGuiNET;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.IO;
using System.Runtime.CompilerServices;
//Modified version of the ImGuiController seen in https://github.com/mellinoe/ImGui.NET/blob/aecc84895f7c02bde31415c470fd460bf60d2045/src/ImGui.NET.SampleProgram/ImGuiController.cs
// It's modified to fit better with Voxelesque's system, instead of using Veldrid.
// The lack of comments REALLY didn't make that any easier, especially since I've never even heard of Veldrid until after this.

//It feels very external to Render, even though it's part of the same library. That's just to make it as generic as possible, and give the Game/App more flexibility.
public sealed class RenderImGuiController{
    //Rendering related objects
    private IRender _render;

    private IRenderMesh _mesh; //The mesh to contain all of the stuff
    //private Matrix4 _projectionMatrix; //Apparently ImGui uses its own projection matrix. I might be able to get away with ignoring it...

    public readonly IRenderTexture _fontTexture;
    private IRenderShader _shader;
    private IRenderEntity _guiEntity; //the "entity" to hook into the Render's rendering system.

    //IO related objects

    private List<char> _inputCharacters;

    public RenderImGuiController(IRender render, IRenderShader shader){
        _render = render;
        //create the ImGui context
        IntPtr context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);
        //set up font
        var fonts = ImGui.GetIO().Fonts;
        fonts.AddFontDefault();
        ImGui.GetIO().BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;


        //This is the mesh that will contain ImGui's render data. 
        _mesh = render.LoadMesh(
            new float[9*4],
            new uint[3*4], 
            new EAttribute[]{
                EAttribute.vec2,//Position
                EAttribute.vec2,//texture coordinates
                EAttribute.vec4,//RGBA color. Yes I'm sending four floats, no I don't really care about performance in this case.
            },
            true //we'll be modifying it regularily so it needs to be a dynamic one.
        );

        //The font texture
        fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height);
        _fontTexture = render.LoadTexture(pixels, width, height, 4);
        fonts.ClearTexData();
        //load the shader
        _shader = shader;


        //This function is SO MUCH shorter and easier to understand than the original code lol
        // That's what a couple of abstractions will do for you.

        //..Oh wait I forgot the last part.
        //The big 'issue' with this sytem is that it has to hook into the main Rendering system, so an entity is used to get a draw call for ImGui.
        //We only use one draw call since we just shove all of the vertex data into a single mesh each frame.
        // One might think the game could delete the entity, but it doesn't have the handle to it so it can't unless whoever wrote it is an absolute idiot and clears very entity.

        _guiEntity = render.SpawnEntity(new EntityPosition(OpenTK.Mathematics.Vector3.Zero, OpenTK.Mathematics.Vector3.Zero, OpenTK.Mathematics.Vector3.One*4000000), _shader, _mesh, _fontTexture, true, null);

        SetKeyMappings();
        SetPerFrameImGuiData((float)_render.Settings.TargetFrameTime);

        ImGui.NewFrame();

        //render.OnRender += OnFrame; //subscribe to the OnRender event.
        render.OnKeyDown += OnKeyDown;

        _inputCharacters = new List<char>();
    }

    private static void SetKeyMappings(){
        ImGuiIOPtr io = ImGui.GetIO();
        io.KeyMap[(int)ImGuiKey.Tab] = (int)Keys.Tab;
        io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Keys.Left;
        io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Keys.Right;
        io.KeyMap[(int)ImGuiKey.UpArrow] = (int)Keys.Up;
        io.KeyMap[(int)ImGuiKey.DownArrow] = (int)Keys.Down;
        io.KeyMap[(int)ImGuiKey.PageUp] = (int)Keys.PageUp;
        io.KeyMap[(int)ImGuiKey.PageDown] = (int)Keys.PageDown;
        io.KeyMap[(int)ImGuiKey.Home] = (int)Keys.Home;
        io.KeyMap[(int)ImGuiKey.End] = (int)Keys.End;
        io.KeyMap[(int)ImGuiKey.Delete] = (int)Keys.Delete;
        io.KeyMap[(int)ImGuiKey.Backspace] = (int)Keys.Backspace;
        io.KeyMap[(int)ImGuiKey.Enter] = (int)Keys.Enter;
        io.KeyMap[(int)ImGuiKey.Escape] = (int)Keys.Escape;
        io.KeyMap[(int)ImGuiKey.Space] = (int)Keys.Space;
        io.KeyMap[(int)ImGuiKey.A] = (int)Keys.A;
        io.KeyMap[(int)ImGuiKey.C] = (int)Keys.C;
        io.KeyMap[(int)ImGuiKey.V] = (int)Keys.V;
        io.KeyMap[(int)ImGuiKey.X] = (int)Keys.X;
        io.KeyMap[(int)ImGuiKey.Y] = (int)Keys.Y;
        io.KeyMap[(int)ImGuiKey.Z] = (int)Keys.Z;
    }

    private void SetPerFrameImGuiData(float deltaSeconds){
        ImGuiIOPtr io = ImGui.GetIO();
        var windowSize = _render.WindowSize();
        io.DisplaySize = new System.Numerics.Vector2(windowSize.X, windowSize.Y);
        io.DisplayFramebufferScale = System.Numerics.Vector2.One;
        io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
    }

    public void Render(double delta){
        SetPerFrameImGuiData((float)delta);
        //Render imgui
        ImGui.Render();
        RenderImDrawData(ImGui.GetDrawData());
        //update inputs and stuff
        ImGuiIOPtr io = ImGui.GetIO();
        MouseState mouse = _render.Mouse();
        KeyboardState keyboard = _render.Keyboard();
        io.MouseDown[0] = mouse.IsButtonDown(MouseButton.Left);
        io.MouseDown[1] = mouse.IsButtonDown(MouseButton.Right);
        io.MouseDown[2] = mouse.IsButtonDown(MouseButton.Middle);
        io.MousePos = ConvertVectors(mouse.Position);
        io.MouseWheel = mouse.ScrollDelta.Y;
        ImGui.NewFrame();
    }

    private void OnKeyDown(KeyboardKeyEventArgs args){
        //RenderUtils.PrintLn(args.Key);
        //_inputCharacters.Add((char)args.Key);
    }

    private System.Numerics.Vector2 ConvertVectors(OpenTK.Mathematics.Vector2 vec){
        return new System.Numerics.Vector2(vec.X, vec.Y);
    }

    private void RenderImDrawData(ImDrawDataPtr imDrawData){
        //RenderUtils.PrintLn(imDrawData.CmdListsCount);
        if (imDrawData.CmdListsCount == 0)
        {
            return;
        }

        List<float> vertices = new List<float>(imDrawData.TotalVtxCount * 9);
        List<uint> indices = new List<uint>(imDrawData.TotalIdxCount);

        for (int i = 0; i < imDrawData.CmdListsCount; i++)
        { //It took me nearly an hour to notice they use a different code style than I do. I suppose I'm just so used to seeing mixed code.
            ImDrawListPtr cmd_list = imDrawData.CmdListsRange[i];
            ImPtrVector<ImDrawVertPtr> VtxBuffer = cmd_list.VtxBuffer;
            ImVector<ushort> IdxBuffer = cmd_list.IdxBuffer;
            //Copy the vertex piece by piece, since it's composed of 4 floats and a uint, and I want that to be 9 floats to be compatible with IRender's system.
            for(int j=0; j<VtxBuffer.Size; j++){
                ImDrawVertPtr vertPtr = VtxBuffer[j];
                vertices.Add(vertPtr.pos.X);
                vertices.Add(vertPtr.pos.Y);
                vertices.Add(vertPtr.uv.X);
                vertices.Add(vertPtr.uv.Y);
                //I suppose I could send it as a bitwise "float", then "convert" it back to an int, but i'm a little too lazy for that.
                // I'm certainly too lazy to rewrite a large portion of my mesh attribute handling system to add integer values lol
                // To be fair to myself, I did JUST rewrite that system entirely, and honestly I'm sick of it
                vertices.Add(((vertPtr.col>>00)&0xFF) / 0xFF);
                vertices.Add(((vertPtr.col>>08)&0xFF) / 0xFF);
                vertices.Add(((vertPtr.col>>16)&0xFF) / 0xFF);
                vertices.Add(((vertPtr.col>>24)&0xFF) / 0xFF);
                //My slinky just broke and i'm sad ):
            }
            //The indices are a lot easier than the vertices (as per usual in the world of indexed meshes)
            for(int j=0; j<IdxBuffer.Size; j++){
                uint index = IdxBuffer[j];
                indices.Add(index);
            }
        }

        ImGuiIOPtr io = ImGui.GetIO();
        imDrawData.ScaleClipRects(io.DisplayFramebufferScale); //Not sure what this does but I'm leaving it in here because I don't want to break anything
        // DON'T Render command lists, because IRender does that for us.
        //Speaking of IRender, we need to actually SEND the data that we converted
        _mesh.ReData(vertices.ToArray(), indices.ToArray());

        //Lol this function is 50% comments
    }
}