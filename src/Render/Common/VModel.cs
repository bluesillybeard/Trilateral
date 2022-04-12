using VQoiSharp;

namespace Voxelesque.Render.Common{
    class VModel{
        public float[] vertices; //elements: xpos, ypos, zpos, xtex, ytex, xnorm, ynorm, znorm
        public uint[] indices;

        public VQoiImage texture;

        public VModel(float[] vertices, uint[] indices, VQoiImage img){
            this.vertices = vertices;
            this.indices = indices;
            this.texture = img;
        }
    }
}