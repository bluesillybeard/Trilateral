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

    public const float ZScale = 0.288675134595f;
    /**
    <summary>
    converts a world position into a chunk position.
    </summary>
    */
    public static Vector3i GetChunkPos(Vector3 worldPos)
    {
        return new Vector3i(
            (int)MathF.Floor(worldPos.X/(Chunk.Size*0.5f)), 
            (int)MathF.Floor(worldPos.Y/(Chunk.Size*0.5f)), 
            (int)MathF.Floor(worldPos.Z/(Chunk.Size*ZScale))
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
            (chunkPos.X+0.5f)*(Chunk.Size*0.5f),
            (chunkPos.Y+0.5f)*(Chunk.Size*0.5f),
            (chunkPos.Z+0.5f)*(Chunk.Size*ZScale)
        );
    }
    /**
    <summary>
    gets the block position given a world position.
    </summary>
    */
    public static Vector3i GetBlockPos(Vector3 worldPos)
    {
        return new Vector3i(
            (int)MathF.Floor(worldPos.X/0.5f),
            (int)MathF.Floor(worldPos.Y/0.5f),
            (int)MathF.Floor(worldPos.Z/ZScale)
        );
    }
    /**
    <summary>
    gets the world position of the center of a block,
    </summary>
    */
    public static Vector3 GetBlockWorldPos(Vector3i blockPos)
    {
        return new Vector3(
            (blockPos.X+0.5f)*0.5f,
            (blockPos.Y+0.5f)*0.5f,
            (blockPos.Z+0.5f)*ZScale
        );
    }
}