using OpenTK.Graphics.OpenGL;

using System;
using System.Collections.Generic;

namespace Voxelesque.Render.GL33{

    struct GL33MeshHandle{

        public GL33MeshHandle(int indexBuffer, int vertexBufferObject, int vertexArrayObject){
            this.indexBuffer = indexBuffer;
            this.vertexBufferObject = vertexBufferObject;
            this.vertexArrayObject = vertexArrayObject;
        }
        public int indexBuffer;

        public int vertexBufferObject;
        public int vertexArrayObject;
    }

    class GL33Mesh: GL33Object, IRenderMesh, IDisposable{
        bool _deleted;
        private int _indexBuffer;

        private int _vertexBufferObject;

        private int _elementCount;
        
        /**
        <summary>
            Creates a Mesh from an element array
        </summary>
        */
        public GL33Mesh(float[] vertices, uint[] indices){
            _elementCount = indices.Length;
            _id = GL.GenVertexArray();
            GL.BindVertexArray(_id);

            _vertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            _indexBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        }

        public void Bind(){
            GL.BindVertexArray(_id);
        }

        public int ElementCount(){
            return _elementCount;
        }

        ~GL33Mesh(){
            //check to see if it's already deleted - if not, it's been leaked and should be taken care of.
            if(!_deleted){
                //add it to the deleted meshes buffer, since the C# garbage collector won't have the OpenGl context.
                //I am aware of the fact this is spaghetti code. I just can't think of a better way to do it.
                //any time this code is used, it can be safely cast to a GL33 object, since only GL33Objects can be created with a GL33Render.
                List<GL33MeshHandle> deletedMeshes = ((GL33Render)RenderUtils.CurrentRender)._deletedMeshes;
                lock(deletedMeshes)
                    deletedMeshes.Add(new GL33MeshHandle(_indexBuffer, _vertexBufferObject, _id));
            }
        }

        //dispose of a garbage-collected mesh
        public static void Dispose(GL33MeshHandle mesh){
            GL.DeleteBuffer(mesh.vertexBufferObject);
            GL.DeleteBuffer(mesh.indexBuffer);

            GL.DeleteVertexArray(mesh.vertexArrayObject);
        }

        public void Dispose(){
            GL.DeleteBuffer(_vertexBufferObject);
            GL.DeleteBuffer(_indexBuffer);

            GL.DeleteVertexArray(_id);
            _deleted = true;
        }
    }
}