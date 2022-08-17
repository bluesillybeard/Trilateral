using System.Collections.Generic;
using System.IO;
using System;

using StbImageSharp;

namespace libvmodel{
    public class VModel{
        public VMesh mesh;

        public ImageResult texture;

        public VModel(VMesh mesh, ImageResult img){
            this.mesh = mesh;
            this.texture = img;
        }

        /**
        <summary>
        loads a VModel from a vmf file.
        The folder and file are given separately so that we can load the resources properly;
        the assets referenced by the vmf file are reletive to the folder.
        The path of the vmf is defined as 'folder + "/" + file'

        The vmf parameter is for when you also need data from the vmf file.
        </summary>
        */
        public VModel(string folder, string file, out Dictionary<string, string>? vmf, out ICollection<string>? errors){
            string fullPath = folder + "/" + file;
            errors = null;
            try{
                //parse the VMF file
                vmf = Libvmodel.ParseListMap(File.ReadAllText(fullPath), out errors);
                string? ts = null; //temp string

                if(!vmf.TryGetValue("type", out ts)){
                    if(errors == null)errors = new List<string>();
                    errors.Add($"Model {fullPath} has no 'type' paramater - moving on precariously...");
                }
                if(!vmf.TryGetValue("mesh", out ts)){
                    //If there is no shape, we use the complere error model.
                    throw new Exception($"Error loading vmesh from VMF: {fullPath} - no vmesh specified");
                }
                this.mesh = new VMesh(folder + "/" +  ts, out errors);

                if(!vmf.TryGetValue("texture", out ts)){
                    //if there is no texture, we use the error texture
                    if(errors == null)errors = new List<string>();
                    errors.Add($"Error loading texture from VMF: {fullPath} - no texture specified");
                    this.texture = ErrorTexture();
                } else {
                    this.texture = ImageResult.FromMemory(File.ReadAllBytes(folder + "/" + ts));
                }

            }catch(Exception e){
                //something went very wrong.
                if(errors == null) errors = new List<string>();
                errors.Add($"Fatal error loading {fullPath}: \n {e}");
                if(this.mesh == null)this.mesh = new VMesh(); //error mesh
                if(this.texture == null)this.texture = ErrorTexture(); //error texture
                vmf = null;
            }
        }

        public VModel(string folder, string file, out Dictionary<string, string>? vmf, out byte[]? removableTriangles, out byte blockedFaces, out ICollection<string>? errors){
            //Most of the code is the same, I don't care.
            string fullPath = folder + "/" + file;
            blockedFaces = 0;
            removableTriangles = null;
            try{
                //parse the VMF file
                vmf = Libvmodel.ParseListMap(File.ReadAllText(fullPath), out errors);
                string? ts = null; //temp string

                if(!vmf.TryGetValue("type", out ts)){
                    if(errors == null)errors = new List<string>();
                    errors.Add($"Model {fullPath} has no 'type' paramater - moving on precariously...");
                }
                if(!vmf.TryGetValue("mesh", out ts)){
                    //If there is no shape, we throw an exception to use the fully errored model.
                    throw new Exception($"Error loading vmesh from VMF: {fullPath} - no vmesh specified");
                }
                this.mesh = new VMesh(folder + "/" +  ts, out errors);
                removableTriangles = mesh.removableTriangles;

                if(!vmf.TryGetValue("texture", out ts)){
                    //if there is no texture, we use the error texture
                    if(errors == null) errors = new List<string>();
                    errors.Add($"Error loading texture from VMF: {fullPath} - no texture specified");
                    this.texture = ErrorTexture();
                } else {
                    this.texture = ImageResult.FromMemory(File.ReadAllBytes(folder + "/" + ts));
                }

                if(!vmf.TryGetValue("blocks", out ts)){
                    blockedFaces = 0;
                    //RenderUtils.printWarnLn($"No blocked faces specified: {fullPath}. Using default value '0'");
                } else {
                    blockedFaces = byte.Parse(ts);
                }



            }catch(Exception e){
                //something went very wrong.
                errors = new List<string>();
                errors.Add($"Fatal error loading {fullPath}: {e}");
                if(this.mesh == null)this.mesh = new VMesh(); //error mesh
                if(this.texture == null)this.texture = ErrorTexture(); //error texture
                if(removableTriangles == null)removableTriangles = mesh.removableTriangles;
                vmf = null;
            }
        }
        private ImageResult ErrorTexture(){
            ImageResult img = new ImageResult();
            img.Height = 2;
            img.Width = 2;
            img.Comp = ColorComponents.RedGreenBlue;
            img.Data = new byte[]{
                255, 0  , 255, 0  , 0  , 0  ,
                0, 0, 0  , 0  , 0  , 255, 0  , 255, //Don't ask me where the extra 2 bytes came from, for some reason it doesn't look right without them.
            };
            img.SourceComp = ColorComponents.RedGreenBlue;
            return img;
        }
    }
}