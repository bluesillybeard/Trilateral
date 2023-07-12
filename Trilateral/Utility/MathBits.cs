namespace Trilateral.Utility;

using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.ComponentModel.Design;
using System.Linq;
using vmodel;
using World;
public static class MathBits
{
    //General math functions

    //These modulus functions do an actual modulus instead of whatever nonsense C# has by default.
    public static int Mod(int num, int div)
    {
        return num < 0 ? ((num+1)%div + div-1) : (num%div);
    }

    public static uint Mod(int num, uint div)
    {
        return (uint)(num < 0 ? ((num+1)%div + div-1) : (num%div));
    }

    public static Vector3i Mod(Vector3i num, int div)
    {
        return new Vector3i(Mod(num.X, div), Mod(num.Y, div), Mod(num.Z, div));
    }

    public static uint Mod(int num, ushort div)
    {
        return (uint)(num < 0 ? ((num+1)%div + div-1) : (num%div));
    }
    //There is nothing wrong with C#'s division operator, in fact it does exactly what it needs to.
    // However, it is sometimes useful to floor instead of  truncate the answer, and that is what this does.

    public static int DivideFloor(int num, int div)
    {
        return (int)MathF.Floor((float)num/div);
    }


    public static Vector3i DivideFloor(Vector3i numerator, int denominator)
    {
        return new Vector3i(
            (int)MathF.Floor(((float)numerator.X)/denominator), 
            (int)MathF.Floor(((float)numerator.Y)/denominator), 
            (int)MathF.Floor(((float)numerator.Z)/denominator));
    }

    //the height of an isometric triangle whose side lengths are each 0.5
    // Solved using pythagoras' theorum.
    // (0.5^2 = 0.25^2 + XScale^2) -> XScale = 0.5/sqrt(0.75)
    public const float XScale = 0.43301270189193864676f; //sqrt(0.75)/2
    /**
    <summary>
    converts a world position into a chunk position.
    </summary>
    */
    public static Vector3i GetChunkPos(Vector3 worldPos)
    {
        return new Vector3i(
            (int)MathF.Floor(worldPos.X/(Chunk.Size*XScale)), 
            (int)MathF.Floor(worldPos.Y/(Chunk.Size*0.5f  )), 
            (int)MathF.Floor(worldPos.Z/(Chunk.Size*0.25f ))
        );
    }
    /**
    <summary>
    gets the world pos of the center of a chunk.
    </summary>
    */
    public static Vector3 GetChunkWorldPos(Vector3i chunkPos)
    {
        return new Vector3(
            (chunkPos.X+0.5f)*(Chunk.Size*XScale),
            (chunkPos.Y+0.5f)*(Chunk.Size*0.5f),
            (chunkPos.Z+0.5f)*(Chunk.Size*0.25f)
        );
    }
    /**
    <summary>
    gets the world pos of the center of a chunk.
    </summary>
    */
    public static Vector3 GetChunkWorldPosUncentered(Vector3i chunkPos)
    {
        return new Vector3(
            chunkPos.X*(Chunk.Size*XScale),
            chunkPos.Y*(Chunk.Size*0.5f),
            chunkPos.Z*(Chunk.Size*0.25f)
        );
    }

    /**
    <summary>
    gets the world pos of the center of a chunk.
    </summary>
    */
    public static Vector3 GetChunkWorldPosUncentered(int cx, int cy, int cz)
    {
        return new Vector3(
            cx*(Chunk.Size*XScale),
            cy*(Chunk.Size*0.5f),
            cz*(Chunk.Size*0.25f)
        );
    }
    /**
    <summary>
    gets the block position given a world position.
    </summary>
    */
    public static Vector3i GetBlockPos(Vector3 worldPos)
    {
        //If there is some genius out there who can make this method faster,
        // please help me! I don't know what i'm doing.
        // TODO: optimize, theoretically rewrite so it doesn't ust this lazy method
        Vector2 wxz = worldPos.Xz;
        //Go through every possible nearby position
        var possibleX = (int)(worldPos.X/XScale);
        var possibleZ = (int)(worldPos.Z/0.25f);
        var x0 = possibleX-2;
        var x1 = possibleX+2;
        var z0 = possibleZ-2;
        var z1 = possibleZ+2;
        int nearestX = 0;
        int nearestZ = 0;
        float nearestDistanceSquared = float.PositiveInfinity;
        for(int x=x0+1; x<x1; x++)
        {
            for(int z=z0+1; z<z1; z++)
            {
                Vector2 wp = GetBlockWorldPos(x, 0, z).Xz;
                float distanceSquared = Vector2.DistanceSquared(wp, wxz);
                if(distanceSquared < nearestDistanceSquared)
                {
                    nearestDistanceSquared = distanceSquared;
                    nearestX = x;
                    nearestZ = z;
                }
            }
        }
        return new Vector3i(
            nearestX,
            //Y position is easy since it's all squares
            (int)MathF.Round(worldPos.Y/0.5f),
            nearestZ
        );
    }
    /**
    <summary>
    gets the world position of the center of a block,
    </summary>
    */
    public static Vector3 GetBlockWorldPos(Vector3i blockPos)
    {
        return GetBlockWorldPos(blockPos.X, blockPos.Y, blockPos.Z);
    }

    public static Vector3 GetBlockWorldPos(int bx, int by, int bz)
    {
        //I'm sincerely sorry, I don't kbow how to explain what this is doing exactly.
        // All you need to know is that it accounts for the tesselation of triangles.
        var parity = ((bx+bz) & 1) == 1;
        var XOffset = -0.072f;
        if(parity)
        {
            //TODO: calculate this offset to greater accuruacy
            XOffset = 0.072f;
        }
        return new Vector3(
            bx * XScale + XOffset,
            by * 0.5f,
            bz * 0.25f
        );
    }
    /**
    <summary>
    Gets the chunk that a block pos is within
    </summary>
    */
    public static Vector3i GetBlockChunkPos(Vector3i blockPos)
    {
        //One would think we could just divide by Chunk.Size and call it a day.
        // But, integer division has an anomaly at 0, since the output is truncated instead of floored.
        // So, we have to convert it into a float, divide by Chunk.Size, floor it, then cast it into an int.
        Vector3 pf = (((Vector3)blockPos)/Chunk.Size);
        return new Vector3i((int)MathF.Floor(pf.X), (int)MathF.Floor(pf.Y), (int)MathF.Floor(pf.Z));
    }

    /**
    <summary>
    Gets the chunk that a block pos is within
    </summary>
    */
    public static Vector3i GetChunkBlockPos(Vector3i chunkPos)
    {
        //One would think we could just divide by Chunk.Size and call it a day.
        // But, integer division has an anomaly at 0, since the output is truncated instead of floored.
        // So, we have to convert it into a float, divide by Chunk.Size, floor it, then cast it into an int.
        Vector3 pf = (((Vector3)chunkPos)*Chunk.Size);
        return new Vector3i((int)MathF.Floor(pf.X), (int)MathF.Floor(pf.Y), (int)MathF.Floor(pf.Z));
    }

    public static Vector3i GetWorldBlockPos(WorldPos pos)
    {
        return GetBlockPos(pos.offset) + GetChunkBlockPos(pos.chunk);
    }

    public static bool MeshRaycast(VMesh mesh, Matrix4 transform, Vector2 pos, out Exception? exception)
    {
        try
        {
            //Check to make sure this mesh as a position component
            if(!mesh.attributes.Contains(EAttribute.position))
            {
                throw new Exception("Mesh does not contain a position attribute");
            }
            uint positionOffset = 0;
            foreach(EAttribute e in mesh.attributes)
            {
                if(e != EAttribute.position)
                {
                    positionOffset += (uint)e %5;
                }
            }
            //for each triangle
            for(uint triangleIndex = 0; triangleIndex < mesh.indices.Length/3; triangleIndex++)
            {
                //Get the vertices from the mesh
                uint vertexIndex = triangleIndex*3;
                uint v1i = mesh.indices[vertexIndex];
                uint v2i = mesh.indices[vertexIndex+1];
                uint v3i = mesh.indices[vertexIndex+2];
                Span<float> v1s = mesh.GetVertex(v1i);
                Span<float> v2s = mesh.GetVertex(v2i);
                Span<float> v3s = mesh.GetVertex(v3i);
                //extract the positions
                Vector3 v1 = new Vector3(v1s[(int)positionOffset], v1s[(int)positionOffset+1], v1s[(int)positionOffset+2]);
                Vector3 v2 = new Vector3(v2s[(int)positionOffset], v2s[(int)positionOffset+1], v2s[(int)positionOffset+2]);
                Vector3 v3 = new Vector3(v3s[(int)positionOffset], v3s[(int)positionOffset+1], v3s[(int)positionOffset+2]);
                //transform them by the matrix
                v1 = Vector3.TransformPerspective(v1, transform);
                v2 = Vector3.TransformPerspective(v2, transform);
                v3 = Vector3.TransformPerspective(v3, transform);
                //see if the triangle collides
                if(v1.Z < 1.0f && v2.Z < 1.0f && v3.Z < 1.0f && PointInTriangle(v1.Xy, v2.Xy, v3.Xy, pos))
                {
                    exception = null;
                    return true;
                }
            }
            exception = null;
            return false;
        } catch( Exception e)
        {
            exception = e;
            return false;
        }
    }

    //thanks to https://www.tutorialspoint.com/Check-whether-a-given-point-lies-inside-a-Triangle for the following code
    //I adapted it to fit my code better, and to fix a bug related to float precision

    public static double TriangleArea(Vector2 A, Vector2 B, Vector2 C) {
        return MathF.Abs((A.X * (B.Y - C.Y) + B.X * (C.Y - A.Y) + C.X * (A.Y - B.Y)) / 2.0f);
    }

    public static bool PointInTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P) {
        double area = TriangleArea (A, B, C) + .0000177;          ///area of triangle ABC //with a tiny bit of extra to avoid issues related to float precision errors
        double area1 = TriangleArea (P, B, C);         ///area of PBC
        double area2 = TriangleArea (A, P, C);         ///area of APC
        double area3 = TriangleArea (A, B, P);        ///area of ABP

        return (area >= area1 + area2 + area3);        ///when three triangles are forming the whole triangle
        //I changed it to >= because floats cannot be trusted to hold perfectly accurate data,
    }
}