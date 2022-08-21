using libvmodel;

using System;
namespace Render{
    public interface IRenderMesh: IRenderObject{
        int ElementCount();
        int VertexCount();
        void ReData(float[] vertices, uint[] indices);
        void AddData(float[] vertices, uint[] indices);
    }
}