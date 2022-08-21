namespace Render;

using OpenTK.Mathematics;
using OpenTK.Windowing;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;


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
// The lack of comments REALLY doesn't help, especially since I've never even heard of Veldrid until now.
public class ImGuiController{
    //Rendering related objects
    private IRender _render;

    private IRenderMesh _mesh;
    private Matrix4 _projectionMatrix;

    private IRenderTexture _fontTexture;
    private IRenderShader _shader;
    private IRenderEntity _guiEntity; //the "entity" to hook into the Render's rendering system.

    //Veldrid has all of its "stuff" surrounding the actual GPU-bound objects, compared to Voxelesque's Render API which has relatively barebones requirements and little abstraction.
    //Honestly I really dislike the "library has it all" approach, since it strangely limits what I feel like I can do. there's always "The way" to do something, 
    // and when I need to specifically do something similar but slightly different, I find I have to do all kinds of weird nonsense to get exactly what I want to happen.
    //Veldrid seems to take the exact opposite approach, where it has SO MUCH raw functionality that it becomes cumbersome to use, although has great power and customizability.
    //It's so far in the opposite direction that it almost seems harder than just doing it in plain OpenGL 3.3 then porting that to other APIs manually lol

    //IO related objects

    public ImGuiController(IRender render){
        _render = render;
        //create the ImGui context
        IntPtr context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);
        //set up font
        var fonts = ImGui.GetIO().Fonts;
        fonts.AddFontDefault();
        ImGui.GetIO().BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        //a huge downside that Voxelesque Render has is that it's utterly incapible of using a different type of mesh than the type defined in a IRenderMesh.
        //Only meshes with 8 vertex attributes and triangle primitives are allowed. Any other type of mesh must be converted.
        _mesh = render.LoadMesh(new float[3*8*1024], new uint[3*1024]);

        fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height);
        _fontTexture = render.LoadTexture(pixels, width, height, 4);
        fonts.ClearTexData();
        _shader = render.LoadShader("ImGui"); //todo: create ImGuivertex.glsl and ImGuifragment.glsl

        //This function is SO MUCH shorter and easier to understand than the original code lol
        // That's what a couple of abstractions will do for you.
        // I think part of the issue is that Veldrid has all of this stuff that is completely unnessesary imo.

        //..Oh wait I forgot the last part.
        //The big 'issue' with this sytem is that it has to hook into the main Rendering system, so an entity is used to get a draw call for ImGui.
        //We only use one draw call since we just shove all of the vertex data into a single mesh each frame.
        // This means that theoretically the game can delete the entity, causing all kinds of havok. But, that's not too big of a problem.

        _guiEntity = render.SpawnEntity(EntityPosition.Zero, _shader, _mesh, _fontTexture, true, null);

        SetKeyMappings();
        SetPerFrameImGuiData((float)_render.Settings.TargetFrameTime);

        ImGui.NewFrame();

        render.OnRender += OnFrame; //subscribe to the OnRender event.
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
        //io.DisplayFramebufferScale = _scaleFactor;
        io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
    }

    private void OnFrame(double delta){
        ImGuiIOPtr io = ImGui.GetIO();
        var windowSize = _render.WindowSize();
        io.DisplaySize = new System.Numerics.Vector2(windowSize.X, windowSize.Y);

        ImGui.Render();
        RenderImDrawData(ImGui.GetDrawData());
    }

    private void RenderImDrawData(ImDrawDataPtr imDrawData){

        if (imDrawData.CmdListsCount == 0)
        {
            return;
        }

        List<float> vertices = new List<float>();
        List<uint> indices = new List<uint>();

        uint totalVBSize = (uint)(imDrawData.TotalVtxCount * Unsafe.SizeOf<ImDrawVert>());
        if (totalVBSize > _vertexBuffer.SizeInBytes)
        {
            gd.DisposeWhenIdle(_vertexBuffer);
            _vertexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalVBSize * 1.5f), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        }

        uint totalIBSize = (uint)(imDrawData.TotalIdxCount * sizeof(ushort));
        if (totalIBSize > _indexBuffer.SizeInBytes)
        {
            gd.DisposeWhenIdle(_indexBuffer);
            _indexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalIBSize * 1.5f), BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        }

        for (int i = 0; i < imDrawData.CmdListsCount; i++)
        {
            ImDrawListPtr cmd_list = imDrawData.CmdListsRange[i];

            cl.UpdateBuffer(
                _vertexBuffer,
                vertexOffsetInVertices * (uint)Unsafe.SizeOf<ImDrawVert>(),
                cmd_list.VtxBuffer.Data,
                (uint)(cmd_list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>()));

            cl.UpdateBuffer(
                _indexBuffer,
                indexOffsetInElements * sizeof(ushort),
                cmd_list.IdxBuffer.Data,
                (uint)(cmd_list.IdxBuffer.Size * sizeof(ushort)));

            vertexOffsetInVertices += (uint)cmd_list.VtxBuffer.Size;
            indexOffsetInElements += (uint)cmd_list.IdxBuffer.Size;
        }

        // Setup orthographic projection matrix into our constant buffer
        ImGuiIOPtr io = ImGui.GetIO();
        Matrix4x4 mvp = Matrix4x4.CreateOrthographicOffCenter(
            0f,
            io.DisplaySize.X,
            io.DisplaySize.Y,
            0.0f,
            -1.0f,
            1.0f);
        GL.
        _gd.UpdateBuffer(_projMatrixBuffer, 0, ref mvp);

        cl.SetVertexBuffer(0, _vertexBuffer);
        cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
        cl.SetPipeline(_pipeline);
        cl.SetGraphicsResourceSet(0, _mainResourceSet);

        imDrawData.ScaleClipRects(io.DisplayFramebufferScale);

        // Render command lists
        int vtx_offset = 0;
        int idx_offset = 0;
        for (int n = 0; n < imDrawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmd_list = imDrawData.CmdListsRange[n];
            for (int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++)
            {
                ImDrawCmdPtr pcmd = cmd_list.CmdBuffer[cmd_i];
                if (pcmd.UserCallback != IntPtr.Zero)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    if (pcmd.TextureId != IntPtr.Zero)
                    {
                        if (pcmd.TextureId == _fontAtlasID)
                        {
                            cl.SetGraphicsResourceSet(1, _fontTextureResourceSet);
                        }
                        else
                        {
                            cl.SetGraphicsResourceSet(1, GetImageResourceSet(pcmd.TextureId));
                        }
                    }

                    cl.SetScissorRect(
                        0,
                        (uint)pcmd.ClipRect.X,
                        (uint)pcmd.ClipRect.Y,
                        (uint)(pcmd.ClipRect.Z - pcmd.ClipRect.X),
                        (uint)(pcmd.ClipRect.W - pcmd.ClipRect.Y));

                    cl.DrawIndexed(pcmd.ElemCount, 1, pcmd.IdxOffset + (uint)idx_offset, (int)pcmd.VtxOffset + vtx_offset, 0);
                }
            }
            vtx_offset += cmd_list.VtxBuffer.Size;
            idx_offset += cmd_list.IdxBuffer.Size;
        }
    }
}