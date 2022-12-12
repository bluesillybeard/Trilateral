namespace vmodel;

using System;
using System.Collections.Generic;
using System.IO;

using StbImageSharp;
public class VModelUtils{
    public static Dictionary<string, string> ParseListMap(string listContents, out List<VError>? errors){
        //TODO: write a better version that handles escape characters properly.
        //TODO: use something 'proper' like a YAML loader or something.
        errors = null;

        //first, we split each non-empty line into it's own string.
        string[] lines = listContents.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        Dictionary<string, string> dict = new Dictionary<string, string>();
        foreach (string line in lines){
            //for each line, split it into the key and value
            string[] keyValue = line.Split(":", 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if(keyValue.Length != 2){
                if(errors == null)errors = new List<VError>(4);
                errors.Add(new VError($"Can't have empty value: \"{line}\". Are you missing a colon or forgot to define it?"));
            } else {
                string value = keyValue[1];
                string key = keyValue[0];
                //then, add them to the dictionary.
                dict.TryAdd(key, value);
            }
        }
        return dict;
    }
    public static Tuple<string, string> SplitFolderAndFile(string path){
        int lastSlash = path.LastIndexOf('/');
        if(lastSlash == -1)return new Tuple<string, string>("", path);
        //we want to include the last slash in the folder, and exclude it in the path
        return new Tuple<string, string>(path.Substring(0, lastSlash+1), path.Substring(lastSlash+1));
    }

    public static VMesh? LoadMesh(string path, out Exception? error){
        byte[] file;
        try{
            file = File.ReadAllBytes(path);
            return LoadMesh(file, out error);
        }catch(Exception e){
            error = e;
            return null;
        }
    }

    public static VMesh? LoadMesh(byte[] file, out Exception? error){
        try{
            int index = 0; //index keeps track of what byte we are on
            //first we get the header info
            uint numVerts = BitConverter.ToUInt32(file, index);
            index+=4;
            if(numVerts == 0){
                error = new InvalidDataException("Invalid vmesh file: cannot have 0 vertices");
                return null;
            }
            uint numTris = BitConverter.ToUInt32(file, index);
            index+=4;
            uint numInds = numTris*3;
            if(numTris == 0){
                error = new InvalidDataException("Invalid vmesh file: cannot have 0 triangles");
                return null;
            }
            uint numAttributes = BitConverter.ToUInt32(file, index);
            index +=4;
            if(numAttributes == 0){
                error = new InvalidDataException("Invalid vmesh file: cannot have 0 attributes");
                return null;
            }
            //attributes
            EAttribute[] attributes = new EAttribute[numAttributes];
            uint totalAttributes = 0;
            for(int i=0; i<numAttributes; i++){
                //for each attribute, read it in.
                attributes[i] = (EAttribute)BitConverter.ToUInt32(file, index);
                totalAttributes += (uint)attributes[i];
                index+=4;
            }
            //file size check
            if(12/*header*/ + totalAttributes*numVerts*4 + numInds*4 + numAttributes*4 > file.Length){
                error = new InvalidDataException("Invalid vmesh file: file too small");
                return null;
            }
            //vertices
            float[] vertices = new float[numVerts*totalAttributes];
            for(int i=0; i<numVerts*totalAttributes; i++){
                vertices[i] = BitConverter.ToSingle(file, index);
                index+=4;
            }
            //indices
            uint[] indices = new uint[numInds];
            for(int i=0; i<numInds; i++){
                indices[i] = BitConverter.ToUInt32(file, index);
                index+=4;
            }
            //removable triangles, if they exist:
            byte[]? removableTris = null;
            if(12/*header*/ + totalAttributes*numVerts*4 + numInds*4 + numAttributes*4 > file.Length){
                removableTris = new byte[numTris];
                for(int i=0; i<numTris; i++){
                    removableTris[i] = file[index];
                    index++;
                }
            }
            //Construct the mesh with the data
            error = null;
            return new VMesh(vertices, indices, attributes, removableTris);
        }catch(Exception e){
            error = e;
            return null;
        }
    }

    public static VModel? LoadModel(string path, out List<VError>? errors){
        errors = null;
        string file;
        try{
            file = File.ReadAllText(path);
            return ParseLoadModel(file, out errors, path);
        }catch(Exception e){
            if(errors == null)errors = new List<VError>(4);
            errors.Add(new VError(e));
            return null;
        }
    }

    public static VModel? ParseLoadModel(string file, out List<VError>? errors, string path){
        errors = null;
        try{
            //parse the VMF file to get the elements
            Dictionary<string, string> vmf = ParseListMap(file, out errors);
            //Get the important values
            string? typeStr = null;
            string? meshStr = null;
            string? blocksStr = null;
            string? blockableStr = null;
            string? textureStr = null;
            if(!vmf.TryGetValue("type", out typeStr)){
                if(errors == null) errors = new List<VError>(4);
                errors.Add(new VError("type parameter not specified in \"" + path + "\""));
                return null;
            }
            if(!vmf.TryGetValue("mesh", out meshStr)){
                if(errors == null) errors = new List<VError>(4);
                errors.Add(new VError("mesh parameter not specified in \"" + path + "\""));
                return null;
            }
            if(typeStr.Equals("block") && !vmf.TryGetValue("blocks", out blocksStr)){
                if(errors == null) errors = new List<VError>(4);
                errors.Add(new VError("blocks parameter not specified in \"" + path + "\""));
                return null;
            }
            if(typeStr.Equals("block") && !vmf.TryGetValue("blockable", out blockableStr)){
                if(errors == null) errors = new List<VError>(4);
                errors.Add(new VError("blockable parameter not specified in \"" + path + "\""));
                return null;
            }
            if(!vmf.TryGetValue("texture", out textureStr)){
                if(errors == null) errors = new List<VError>(4);
                errors.Add(new VError("texture parameter not specified in \"" + path + "\""));
                return null;
            }
            byte? blocks = null;
            byte? blockable = null;
            //Now we get the actual data
            if(typeStr.Equals("block")){
                if(!byte.TryParse(blocksStr, out byte blockst)){
                    if(errors == null) errors = new List<VError>(4);
                    errors.Add(new VError("unable to parse blocks parameter in \"" + path + "\""));
                    return null;
                }
                blocks = blockst;
                 if(!byte.TryParse(blockableStr, out blockst)){
                    if(errors == null) errors = new List<VError>(4);
                    errors.Add(new VError("unable to parse blockable parameter in \"" + path + "\""));
                    return null;
                }
                blockable = blockst;
            }
            var pathSep = SplitFolderAndFile(path);
            VMesh? mesh = LoadMesh(File.ReadAllBytes(pathSep.Item1 + meshStr), out var exception);
            if(exception != null){
                if(errors==null)errors = new List<VError>(4);
                errors.Add(new VError(exception));
                return null;
            }
            if(mesh == null){
                throw new Exception("INVALID STATE: Mesh from LoadMesh shouldn't be null here under any circumstances!!");
            }
            ImageResult texture = ImageResult.FromMemory(File.ReadAllBytes(pathSep.Item1 + textureStr));
            //Finally, we place all the stuff into a model and return itt
            return new VModel(mesh.Value, texture, blocks, blockable);
        }catch(Exception e){
            if(errors == null)errors = new List<VError>(4);
            errors.Add(new VError(e));
            return null;
        }
    }
}