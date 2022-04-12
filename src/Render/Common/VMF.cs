using System.Collections.Generic;
using System.IO;
using System;

using VQoiSharp;
using VQoiSharp.Codec;
namespace Voxelesque.Render.Common{
    class VMF{
        public static VModel LoadVEMF(string folder, string file){
            try{
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

                //load the texture
                ts = null;
                vmf.TryGetValue("texture", out ts);

                byte[] rawData;
                int width;
                int height;
                VQoiChannels channels;
                if(ts == null){
                    //if it couldn't get the texture path, use the classic magneta & black error texture.
                    RenderUtils.printErrLn("error loading texture from VMF: " + folder + "/" + file + " - no texture specified");
                    rawData = new byte[]{
                        255, 0, 255, 0,   0, 0,
                        0,   0, 0,   255, 0, 255
                    };
                    width = 2;
                    height = 2;
                    channels = VQoiChannels.Rgb;
                } else{
                    rawData = RenderUtils.GetRawImageData(folder + ts, out width, out height, out channels);
                }

                //put it all together
                return new VModel(vertices, indices, new VQoiImage(rawData, width, height, channels));
            } catch(Exception e){
                //If something went horribly wrong, return the error mesh - a flat square with the error texture and no surface normal
                RenderUtils.printErrLn(e);
                return new VModel(
                    new float[]{
                         1,  1, 0, 1, 1, 0, 0, 0,
                        -1,  1, 0, 0, 1, 0, 0, 0,
                         1, -1, 0, 1, 0, 0, 0, 0,
                        -1, -1, 0, 0, 0, 0, 0, 0,
                    }, 
                    new uint[]{
                        0, 1, 2,
                        1, 2, 3,
                    },
                    new VQoiImage(
                        new byte[]{
                            255, 0, 255, 0,   0, 0,
                            0,   0, 0,   255, 0, 255
                        }, 2, 2, VQoiChannels.Rgb
                    )
                );
            }
        }
    }
}