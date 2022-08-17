using System.IO;
using System;
using System.Collections.Generic;
namespace libvmodel{
    public class VMesh{
        public float[] vertices; //elements: xpos, ypos, zpos, xtex, ytex, xnorm, ynorm, znorm
        public uint[] indices;

        public byte[]? removableTriangles;
        //public byte blockedFaces; blocked faces are part of the vmf file, not the vbmesh.

        public VMesh(float[] vertices, uint[] indices){
            this.vertices = vertices;
            this.indices = indices;
            removableTriangles = null;
        }

        public VMesh(string vmeshPath, out ICollection<string>? errors){
            errors = null;
            byte[] vmesh;
            try{
                vmesh = File.ReadAllBytes(vmeshPath);
            } catch (Exception e){
                errors = new List<string>();
                errors.Add("Error loading vmesh file: " + e);
                Error(out vertices, out indices);
                return;
            }
            try{
                //copy the vertices one float at a time. The vmesh already has the attributes in the right order, so we don't have to worry about that.
                vertices = new float[8 * BitConverter.ToInt32(vmesh, 0)];
                for(int i=0; i<vertices.Length; i++){
                    vertices[i] = BitConverter.ToSingle(vmesh, i*4+12); //header is 12 bytes long
                }

                //do with the indices the same as the vertices.
                indices = new uint[BitConverter.ToInt32(vmesh, 4)];

                for(int i=0; i<indices.Length; i++){
                    indices[i] = BitConverter.ToUInt32(vmesh, 4*vertices.Length + 4*i + 12);
                }

                //if we have enough data for the removable triangles, we'll load them.
                if(vmesh.Length >= 4*vertices.Length + 4*indices.Length + indices.Length/3 + 12){
                    removableTriangles = new byte[indices.Length/3];
                    for(int i=0; i<removableTriangles.Length; i++){
                        removableTriangles[i] = vmesh[4*vertices.Length + 4*indices.Length + 12 + i];
                    }
                } else {
                    removableTriangles = null;
                }
            } catch (Exception){
                errors = new List<string>();
                errors.Add("Malformed vmesh file: " + vmeshPath);
                Error(out vertices, out indices);
            }
        }

        /**
        <summary>
        create the 'error mesh' - a unit square on the XY plane with no surface normal.
        </summary>
        */
        public VMesh(){
            Error(out vertices, out indices);
        }

        private static void Error(out float[] vertices, out uint[] indices){
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