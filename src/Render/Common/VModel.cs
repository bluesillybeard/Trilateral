namespace Voxelesque.Render.Common{
    class VModel{
        public float[] vertices; //elements: xpos, ypos, zpos, xtex, ytex, xnorm, ynorm, znorm
        public uint[] indices;

        public byte[] imgData;
        public uint imgWidth, imgHeight;

        public VModel(float[] vertices, uint[] indices, byte[] imgData, uint imgWidth, uint imgHeight){
            this.vertices = vertices;
            this.indices = indices;
            this.imgData = imgData;
            this.imgWidth = imgWidth;
            this.imgHeight = imgHeight;
        }
    }
}