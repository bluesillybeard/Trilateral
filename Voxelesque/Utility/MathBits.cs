namespace Voxelesque.Utility;

using OpenTK.Mathematics;
using System;
using World;
public static class MathBits
{

    public static int Mod(int num, int div)
    {
        return num < 0 ? ((num+1)%div + div-1) : (num%div);
    }

    public static uint Mod(int num, uint div)
    {
        return (uint)(num < 0 ? ((num+1)%div + div-1) : (num%div));
    }

    public static uint Mod(int num, ushort div)
    {
        return (uint)(num < 0 ? ((num+1)%div + div-1) : (num%div));
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
    gets the block position given a world position.
    </summary>
    */
    public static Vector3i GetBlockPos(Vector3 worldPos)
    {

        //I'm sincerely sorry, I don't know how to explain what this is doing exactly.
        // All you need to know is that it accounts for the tesselation of triangles.
        int z = (int)MathF.Round(worldPos.Z/0.25f);
        int xp = (int)MathF.Round((worldPos.X/*-XOffset*/)/MathBits.XScale);
        //TODO: finish implementing this
        // The Z parity is easy to calculate,
        // But the X parity may not be so simple.

        var XOffset = -0.072f;
        var XParity = (xp & 1) == 1;
        var ZParity = (z & 1) == 1;
        if(XParity ^ ZParity)
        {
            //TODO: calculate this offset to greater accuruacy
            XOffset = 0.072f;
        }
        return new Vector3i(
            (int)MathF.Round((worldPos.X-XOffset)/MathBits.XScale),
            (int)MathF.Round(worldPos.Y/0.5f),
            z
        );
    }
    /**
    <summary>
    gets the world position of the cetner of a block,
    </summary>
    */
    public static Vector3 GetBlockWorldPos(Vector3i blockPos)
    {
        //I'm sincerely sorry, I don't kbow how to explain what this is doing exactly.
        // All you need to know is that it accounts for the tesselation of triangles.
        var parity = ((blockPos.X+blockPos.Z) & 1) == 1;
        var XOffset = -0.072f;
        if(parity)
        {
            //TODO: calculate this offset to greater accuruacy
            XOffset = 0.072f;
        }
        return new Vector3(
            blockPos.X * MathBits.XScale + XOffset,
            blockPos.Y * 0.5f,
            blockPos.Z * 0.25f
        );


        /*
        //I'm sincerely sorry, I don't kbow how to explain what this is doing exactly.
        // All you need to know is that it accounts for the tesselation of triangles.
        var parity = ((blockPos.X+blockPos.Z) & 1) == 1;
        var XOffset = -0.072f * -((blockPos.X+blockPos.Z) % 2);
        if(parity)
        {
            //TODO: calculate this offset to greater accuruacy
            XOffset = 0.072f;
        }
        return new Vector3(
            blockPos.X * MathBits.XScale + XOffset,
            blockPos.Y * 0.5f,
            blockPos.Z * 0.25f
        );
        */
    }
}