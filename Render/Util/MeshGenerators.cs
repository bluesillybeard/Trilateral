namespace Render.Util;

using vmodel;

using System.Collections.Generic;
public class MeshGenerators{

    //TODO: create a mesh generator class that can be used to append vertices to a final mesh, rather than doing the nonsense you currently have.

    //text is the text to convert
    //centerx and centery tell weather or not to center in certain directions
    //attributes are the output attributes of the mesh.
    //posAttrib and texAttrib are the indices of the position and texture coordinate attributes respectively
    //error is a string that tells what went wrong if this function returns null
    public static VMesh? BasicText(string text, bool centerX, bool centerY, EAttribute[] attributes, int posAttrib, int texAttrib, out string? error){
        //NOTE: This was copied verbatim (and modified for the different way vertices are stored) from my old Java codebase (the 'proof of concept' if you will)
        //that means, this is pretty inneficient and bad code. It will be replaced once I get SDFs to work. (which, let's be honest, is never)

        //Yes, I am aware that this is a really inefficient way of doing it.
        //But, unless we are rendering insane amounts of text, I think it will be fine.

        //COMMENT FROM MODEL REFORM: The model reform made this a little but more complicated, but it's still the same underlying algorithm as in the original Java code.

        //Check that the attribute target is valid
        if(posAttrib >= attributes.Length || texAttrib >= attributes.Length){
            error = ("Invalid attribute index " + posAttrib + "/" + texAttrib + " for attributes {" + string.Join(", ", attributes) + "}");
            return null;
        }
        //sort the text to a more readable form
        List<List<char>> lines = new List<List<char>>();
        List<char> line = new List<char>();
        int numCharacters = 0;
        //split into lines
        foreach(char character in text){
            if(character == '\n'){
                lines.Add(line);
                line = new List<char>();
            } else {
                line.Add(character);
                numCharacters++;
            }
        }
        lines.Add(line);
        //generate the actual mesh

        //Initialize some values to help the algorithm understand the desired attribute output
        int totalAttrib = 0;
        int posAttribOffset = 0;
        int texAttribOffset = 0;
        for(int i=0; i<attributes.Length; i++){
            EAttribute attribute = attributes[i];
            totalAttrib += (int)attribute;
            if(i < posAttrib){
                posAttribOffset += (int)attribute;
            }
            if(i < texAttrib){
                texAttribOffset += (int)attribute;
            }
        }
        if(totalAttrib < 4){
            throw new System.Exception("Not enough attributes");
        }
        int extraAttrib  = totalAttrib - 4;
        //We use the previously mentioned values to create a mapping
        int[] mapping = new int[totalAttrib];
        for(int i=0; i<mapping.Length; i++)mapping[i] = -1; //initialize values to -1
        mapping[posAttribOffset  ] = 0;
        mapping[posAttribOffset+1] = 1;
        mapping[texAttribOffset  ] = 2;
        mapping[texAttribOffset+1] = 3;

        MeshBuilder builder = new MeshBuilder(attributes, totalAttrib, numCharacters*4*totalAttrib, numCharacters*6);
        float YStart = centerY ? -lines.Count/2f : 0; //the farthest up coordinate of the text.
        for(int i=0; i < lines.Count; i++){
            float XStart = centerX ? -lines[i].Count/2f : 0; //the farthest left coordinate of the text.
            for(int j=0; j<lines[i].Count; j++){
                char character = lines[i][j];
                int column = character & 15;
                int row = character >> 4 & 15; //get the last 4 bits and first 4 bits (row and column from the ASCII texture)
                float iXStart = XStart+j;
                float iYStart = YStart-i;
                float UVXPosition = column*0.0625f;
                float UVYPosition = row*0.0625f; //get the actual UV coordinates of the top left corner
                //FIRST TRIANGLE
                builder.AddVertex(mapping, iXStart  , iYStart  , UVXPosition        , UVYPosition        ); //top left
                builder.AddVertex(mapping, iXStart+1, iYStart  , UVXPosition+0.0625f, UVYPosition        ); //top right
                builder.AddVertex(mapping, iXStart  , iYStart-1, UVXPosition        , UVYPosition+0.0625f); //bottom left
                //SECOND TRIANGLE
                builder.AddVertex(mapping, iXStart+1, iYStart  , UVXPosition+0.0625f, UVYPosition        ); //top right
                builder.AddVertex(mapping, iXStart  , iYStart-1, UVXPosition        , UVYPosition+0.0625f); //bottom left
                builder.AddVertex(mapping, iXStart+1, iYStart-1, UVXPosition+0.0625f, UVYPosition+0.0625f); //bottom right
            }
        }
        error = null;
        return builder.ToMesh();
    }    
}