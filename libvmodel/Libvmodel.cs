using System.Collections.Generic;
using System;


namespace libvmodel;
public static class Libvmodel
{
    /**
    <summary>
    Parses a list mapping from a string.
    Each key-value pair is separated by a new line, and a colon separates the key from the value.
    </summary>
    */
    public static Dictionary<string, string> ParseListMap(string listContents, out ICollection<string>? errors){
        //TODO: write a better version that handles escape characters properly.
        //TODO: use something 'proper' like a YAML loader or something.
        errors = null;

        //first, we split each non-empty line into it's own string.
        string[] lines = listContents.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        Dictionary<string, string> dict = new Dictionary<string, string>();
        foreach (string line in lines){
            //for each line, split it into the key and value
            string[] keyValue = line.Split(":", 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if(keyValue.Length < 2){
                if(errors == null)errors = new List<string>();
                errors.Add($"Can't have empty value: \"{line}\". Are you missing a colon or forgot to define it?");
                continue;
            }
            string value = keyValue[1];
            string key = keyValue[0];
            //then, add them to the dictionary.
            dict.TryAdd(key, value);
        }
        return dict;
    }

    public static VMesh LoadMesh(string pathToVmesh, out ICollection<string>? errors, byte blockedFaces){
        return new VMesh(pathToVmesh, out errors, blockedFaces);
    }
}
