using System.Collections.Generic;
using System.IO;
using System;
namespace Voxelesque.Render.Common{
    class VMF{
        public static VModel LoadVEMF(string folder, string file){
            //read the vmf file itself
            Dictionary<string, string> vmf = ListParser.ParseListMap(File.ReadAllText(folder + "/" + file));
            string ts = null; //temp string
            vmf.TryGetValue("type", out ts);
            if(ts == "block"){
                RenderUtils.printErrLn("reading a block mesh as an entity mesh");
            } else if(ts == null){
                RenderUtils.printErrLn("Error parsing VMF: " + folder + "/" + file + " - couldn't find 'type' attibute. Moving on precariously...");
            }
            
            if(!vmf.TryGetValue("mesh", out ts)){
                RenderUtils.printErrLn("Fatal error loading vmesh from VMF: " + folder + "/" + file + " - no vmesh specified");
                return null;
            }
            byte[] vmesh = File.ReadAllBytes(folder + ts);

            //TODO: check endian

            //copy the vertices one float at a time. The vmesh already has the attributes in the right order, so we don't have to worry about that.
            float[] vertices = new float[8 * BitConverter.ToInt32(vmesh, 0)];
            for(int i=0; i<vertices.Length; i++){
                vertices[i] = BitConverter.ToSingle(vmesh, i*4+8);
            }

            //TODO: make sure offsets are correct

            //do with the indices the same as the vertices.
            uint[] indices = new uint[BitConverter.ToInt32(vmesh, 4)];

            for(int i=0; i<indices.Length; i++){
                indices[i] = BitConverter.ToUInt32(vmesh, 4*vertices.Length + 4*i + 8);
            }




            return null;//new VModel(
            //    vertices, indices,
            //    image,
            //    imgWidth, height
            //);
        }
    }
}