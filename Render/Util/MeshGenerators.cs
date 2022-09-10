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
        uint totalAttrib = 0;
        uint posAttribOffset = 0;
        uint texAttribOffset = 0;
        for(int i=0; i<attributes.Length; i++){
            EAttribute attribute = attributes[i];
            totalAttrib += (uint)attribute;
            if(i < posAttrib){
                posAttribOffset += (uint)attribute;
            }
            if(i < texAttrib){
                texAttribOffset += (uint)attribute;
            }
        }

        uint extraAttrib  = totalAttrib - 4;

        float[] vertices = new float[numCharacters*4*totalAttrib]; //4 vertices * 4 elements/vertex * x characters = # of elements
        uint[] indices = new uint[numCharacters*6];
        
        float YStart = centerY ? -lines.Count/2f : 0; //the farthest up coordinate of the text.
        uint charIndex = 0;
        for(int i=0; i < lines.Count; i++){
            float XStart = centerX ? -lines[i].Count/2f : 0; //the farthest left coordinate of the text.
            for(int j=0; j<lines[i].Count; j++){
                char character = lines[i][j];

                int column = character & 15;
                int row = character >> 4 & 15; //get the last 4 bits and first 4 bits (row and column from the ASCII texture)

                uint charElement = charIndex*4*5; //the start of the first element to write to. A pointer, if you will.

                float iXStart = XStart+j;
                float iYStart = YStart-i;

                float UVXPosition = column*0.0625f;
                float UVYPosition = row*0.0625f; //get the actual UV coordinates of the top left corner

                //set vertices
                //This became a whole lot more complicated after the model reform lol
                vertices[charElement   +posAttribOffset              ] = iXStart; //top left
                vertices[charElement+ 1+posAttribOffset              ] = iYStart;
                vertices[charElement+ 2+texAttribOffset              ] = UVXPosition;
                vertices[charElement+ 3+texAttribOffset              ] = UVYPosition;

                vertices[charElement+ 4+posAttribOffset+extraAttrib  ] = iXStart+1; //top right
                vertices[charElement+ 5+posAttribOffset+extraAttrib  ] = iYStart;
                vertices[charElement+ 6+texAttribOffset+extraAttrib  ] = UVXPosition+0.0625f;
                vertices[charElement+ 7+texAttribOffset+extraAttrib  ] = UVYPosition;

                vertices[charElement+ 8+posAttribOffset+extraAttrib*2] = iXStart; //bottom left
                vertices[charElement+ 9+posAttribOffset+extraAttrib*2] = iYStart-1;
                vertices[charElement+10+texAttribOffset+extraAttrib*2] = UVXPosition;
                vertices[charElement+11+texAttribOffset+extraAttrib*2] = UVYPosition + 0.0625f;
                
                vertices[charElement+12+posAttribOffset+extraAttrib*3] = iXStart+1; //bottom right
                vertices[charElement+13+posAttribOffset+extraAttrib*3] = iYStart-1;
                vertices[charElement+14+texAttribOffset+extraAttrib*3] = UVXPosition + 0.0625f;
                vertices[charElement+15+texAttribOffset+extraAttrib*3] = UVYPosition + 0.0625f;

                //indices 0, 1, 2, 1, 2, 3
                uint j6 = charIndex*6;
                indices[j6  ] =   charIndex*4;
                indices[j6+1] = 1+charIndex*4;
                indices[j6+2] = 2+charIndex*4;

                indices[j6+3] = 1+charIndex*4;
                indices[j6+4] = 2+charIndex*4;
                indices[j6+5] = 3+charIndex*4;
                charIndex++;
            }
        }
        error = null;
        return new VMesh(vertices, indices, attributes, null);
    }

    static VMesh ErrorMesh(EAttribute[] attributes, int posAttrib, int texAttrib){
        float[] vertices = new float[]{

        }
    }    
}