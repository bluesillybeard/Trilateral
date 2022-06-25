namespace Render.Util;

using libvmodel;

using System.Collections.Generic;
public class MeshGenerators{
    public static VMesh BasicText(string text, bool centerX, bool centerY){
        //NOTE: This was copied verbatim (and modified for the different way vertices are stored) from my old Java codebase (the 'proof of concept' if you will)
        //that means, this is pretty inneficient and bad code. It will be replaced once I get SDFs to work. (which, let's be honest, is never)

        //Yes, I am aware that this is a really inefficient way of doing it.
        //But, unless we are rendering insane amounts of text, I think it will be fine.

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
        //generate the actual mesh

        //Vertex elements: Xp Yp Zp Xt Yt Xn Yn Zn
        float[] vertices = new float[numCharacters*4*8]; //4 vertices * 8 elements/vertex * x characters = # of elements
        uint[] indices = new uint[numCharacters*6];
        lines.Add(line);

        float YStart = centerY ? -lines.Count/2f : 0; //the farthest up coordinate of the text.
        uint charIndex = 0;
        for(int i=0; i < lines.Count; i++){
            float XStart = centerX ? -lines[i].Count/2f : 0; //the farthest left coordinate of the text.
            for(int j=0; j<lines[i].Count; j++){
                char character = lines[i][j];

                int column = character & 15;
                int row = character >> 4 & 15; //get the last 4 bits and first 4 bits (row and column from the ASCII texture)

                uint charElement = charIndex*4*8; //the start of the first element to write to. A pointer, if you will.

                float iXStart = XStart+j;
                float iYStart = YStart-i;

                float UVXPosition = column*0.0625f;
                float UVYPosition = row*0.0625f; //get the actual UV coordinates of the top left corner

                //set vertices
                //This is the most beautiful (inneficient lol) thing I have ever created.
                vertices[charElement  ] = iXStart; //top left
                vertices[charElement+1] = iYStart;
                //vertices[charElement+2] = 0;
                vertices[charElement+3] = UVXPosition;
                vertices[charElement+4] = UVYPosition;
                //vertices[charElement+5] = 0; //set normals to 0, since text should render from either side.
                //vertices[charElement+6] = 0;
                //vertices[charElement+7] = 0;

                vertices[charElement+8] = iXStart+1; //top right
                vertices[charElement+9] = iYStart;
                //vertices[charElement+10] = 0;
                vertices[charElement+11] = UVXPosition+0.0625f;
                vertices[charElement+12] = UVYPosition;
                //vertices[charElement+13] = 0; //set normals to 0, since text should render from either side.
                //vertices[charElement+14] = 0;
                //vertices[charElement+15] = 0;

                vertices[charElement+16] = iXStart; //bottom left
                vertices[charElement+17] = iYStart-1;
                //vertices[charElement+18] = 0;
                vertices[charElement+19] = UVXPosition;
                vertices[charElement+20] = UVYPosition + 0.0625f;
                //vertices[charElement+21] = 0; //set normals to 0, since text should render from either side.
                //vertices[charElement+22] = 0;
                //vertices[charElement+23] = 0;

                vertices[charElement+24] = iXStart+1; //bottom right
                vertices[charElement+25] = iYStart-1;
                //vertices[charElement+26] = 0;
                vertices[charElement+27] = UVXPosition + 0.0625f;
                vertices[charElement+28] = UVYPosition + 0.0625f;
                //vertices[charElement+29] = 0; //set normals to 0, since text should render from either side.
                //vertices[charElement+30] = 0;
                //vertices[charElement+31] = 0;

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

        return new VMesh(vertices, indices);
    }
}