using System.Collections.Generic;
using System.IO;

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

            //float[] vertices

            //return new VModel(
            //    vertices, indices,
            //    image,
            //    imgWidth, height
            //);
            return null;
        }
    }
}