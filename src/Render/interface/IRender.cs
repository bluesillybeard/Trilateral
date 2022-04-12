using System;
using System.Drawing;

using Voxelesque.Render.Common;

using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

using StbImageSharp;
namespace Voxelesque.Render{
    interface IRender{

        //mixed bits
        bool Init(RenderSettings settings);

        void Run();

        /**
        <summary>
        This action is called every update - 15 times each second by default
        </summary>
        */
        Action<double> OnVoxelesqueUpdate {get; set;}

        RenderSettings Settings{get;}

        //meshes
        IRenderMesh LoadMesh(float[] vertices, uint[] indices);

        /**
         <summary> 
          loads a VMF file as a mesh (.vemf, .vbmf, .vmf)
          More details about how vmf files work in the documentation
         </summary>
        */
        //IRenderMesh LoadMesh(string VMFPath);

        void DeleteMesh(IRenderMesh mesh);
        //textures

        /**
        <summary>
        loads a texture.
        Supports png, jpg, jpeg, qoi, vqoi, vmf, vemf, vbmf
        NOTICE: vmf, vemf, and vbmf simply contain a reference to a texture, and they temselves are not image formats.
        </summary>
        */
        IRenderTexture LoadTexture(string filePath);

        IRenderTexture LoadTexture(ImageResult image);

        void DeleteTexture(IRenderTexture texture);

        //shaders

        /**
        <summary>
        loads, compiles, and links a shader program.

        Note that, for a GL33Render for example, "fragment.glsl" and "vertex.glsl" is appended to the shader path for the pixel and vertex shaders respectively.
        </summary>

        */
        IRenderShader LoadShader(string shaderPath);

        void DeleteShader(IRenderShader shader);

        //models

        /**
        <summary>
        loads the mesh and texture from a vmf, vemf, or vbmf model
        </summary>
        */
        //RenderEntityModel LoadModel(string VMFPath);

        //void DeleteModel(RenderEntityModel model);
        

        //entities

        IRenderEntity SpawnEntity(EntityPosition pos, IRenderShader shader, IRenderMesh mesh, IRenderTexture texture);

        void DeleteEntity(IRenderEntity entity);

        //camera

        RenderCamera SpawnCamera(Vector3 position, Vector3 rotation, float fovy);

        void SetCamera(RenderCamera camera);
        void DeleteCamera(RenderCamera camera);

        //input
        KeyboardState Keyboard();

        MouseState Mouse();

        bool CursorLocked{get; set;}
    }
}