using System.IO;
using System;

namespace Voxelesque.Render.Common{
    class VMesh{
        public float[] vertices; //elements: xpos, ypos, zpos, xtex, ytex, xnorm, ynorm, znorm
        public uint[] indices;

        public VMesh(float[] vertices, uint[] indices){
            this.vertices = vertices;
            this.indices = indices;
        }

        public VMesh(string vmeshPath){
            byte[] vmesh = File.ReadAllBytes(vmeshPath);

            //copy the vertices one float at a time. The vmesh already has the attributes in the right order, so we don't have to worry about that.
            vertices = new float[8 * BitConverter.ToInt32(vmesh, 0)];
            for(int i=0; i<vertices.Length; i++){
                vertices[i] = BitConverter.ToSingle(vmesh, i*4+12); //header is 12 bytes long
            }

            //TODO: make sure offsets are correct

            //do with the indices the same as the vertices.
            indices = new uint[BitConverter.ToInt32(vmesh, 4)];

            for(int i=0; i<indices.Length; i++){
                indices[i] = BitConverter.ToUInt32(vmesh, 4*vertices.Length + 4*i + 12);
            }

        }

        /**
        <summary>
        create the 'error mesh' - a unit square on the XY plane with no surface normal.
        </summary>
        */
        public VMesh(){
            vertices = new float[]{
                1,  1, 0, 1, 1, 0, 0, 0,
                -1,  1, 0, 0, 1, 0, 0, 0,
                1, -1, 0, 1, 0, 0, 0, 0,
                -1, -1, 0, 0, 0, 0, 0, 0,
            };
            indices = new uint[]{
                0, 1, 2,
                1, 2, 3,
            };
        }
    }
}