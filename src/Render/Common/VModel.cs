using System.Collections.Generic;
using System.IO;
using System;

using VQoiSharp;
using VQoiSharp.Codec;
namespace Voxelesque.Render.Common{
    class VModel{
        public VMesh mesh;

        public VQoiImage texture;

        public VModel(VMesh mesh, VQoiImage img){
            this.mesh = mesh;
            this.texture = img;
        }

        public VModel(string folder, string file){
            string fullPath = folder + "/" + file;
            try{
                //parse the VMF file
                Dictionary<string, string> vmf = ListParser.ParseListMap(File.ReadAllText(fullPath));
                string ts = null; //temp string

                if(!vmf.TryGetValue("type", out ts)){
                    RenderUtils.printErrLn($"Model {fullPath} has no 'type' paramater - moving on precariously...");
                }
                if(!vmf.TryGetValue("mesh", out ts)){
                    //If there is no shape, we use the complere error model.
                    throw new Exception($"Error loading vmesh from VMF: {fullPath} - no vmesh specified");
                }
                this.mesh = new VMesh(folder + "/" +  ts);

                if(!vmf.TryGetValue("texture", out ts)){
                    //if there is no texture, we use the error texture
                    RenderUtils.printErrLn($"Error loading texture from VMF: {fullPath} - no texture specified");
                    this.texture = new VQoiImage();
                } else {
                    this.texture = RenderUtils.GetRawImage(folder + "/" + ts);
                }

            }catch(Exception e){
                //something went very wrong.
                RenderUtils.printErr($"Fatal error loading {fullPath}:");
                RenderUtils.printErrLn(e);
                if(this.mesh == null)this.mesh = new VMesh(); //error mesh
                if(this.texture == null)this.texture = new VQoiImage(); //error texture
            }
        }
    }
}