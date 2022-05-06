using System;
using System.Collections.Generic;

using Voxelesque.Render.Common;

using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

using StbImageSharp;

using libvmodel;
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

        Vector2 WindowSize();

        //meshes
        IRenderMesh LoadMesh(float[] vertices, uint[] indices);

        IRenderMesh LoadMesh(VMesh mesh);

        /**
         <summary> 
          loads a vmesh file into a GPU-stored mesh.
         </summary>
        */
        IRenderMesh LoadMesh(string VMFPath);

        void DeleteMesh(IRenderMesh mesh);
        //textures

        /**
        <summary>
        loads a texture into the GPU.
        Supports png, jpg, jpeg, qoi, vqoi
        </summary>
        */
        IRenderTexture LoadTexture(string filePath);

        /**
        <summary>
        loads a texture into the GPU
        </summary>
        */
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
        RenderEntityModel LoadModel(string folder, string file);

        RenderEntityModel LoadModel(VModel model);

        /**
        <summary>
        deletes the internal mesh and texture of a model.
        </summary>
        */

        void DeleteModel(RenderEntityModel model);
        

        //entities

        IRenderEntity SpawnEntity(EntityPosition pos, IRenderShader shader, IRenderMesh mesh, IRenderTexture texture);

        void DeleteEntity(IRenderEntity entity);

        /**
        <summary>
        Returns the list of entities.
        Note that there WILL be null elements. If an entity is 'null', it means that it has been removed.
        </summary>
        */
        IEnumerable<IRenderEntity> GetEntities();
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