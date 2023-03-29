namespace Voxelesque.World;

using System;

public struct Chunk
{
    // Chunks are quite unusually shaped (geometrically speaking), but the data is stored as a cube.
    public const ushort Size = 32;
    //The chunk length is the total number of blocks in a chunk
    const uint Length = Size*Size*Size;

    //when the chunk was last modified
    DateTime lastChange;
    public DateTime LastChange{get=>lastChange;}

    //Can't statically assign a length of an array in C#.
    // It's valid though, since an array can be any length.
    private Block[/*ChunkSize^3*/]? blocks;

    //creates a new empty chunk
    public Chunk()
    {
        this.blocks = null;
        lastChange = DateTime.Now;
    }

    public Chunk(Block[] blocks)
    {
        if(blocks.Length != Length)
        {
            throw new Exception("Cannot create a chunk with the incorrect length!");
        }
        this.blocks = blocks;
        lastChange = DateTime.Now;
    }
    public Block? GetBlock(uint x, uint y, uint z)
    {
        uint index = x + Size*y + Size*Size*z;
        return GetBlock(index);
    }
    public Block? GetBlock(uint index)
    {
        if(this.blocks == null)
        {
            return null;
        }
        return this.blocks[index];
    }
    public void SetBlock(Block block, uint index)
    {
        if(this.blocks is null)
        {
            this.blocks = new Block[Length];
        }
        this.blocks[index] = block;
        lastChange = DateTime.Now;
    }
    public void SetBlock(Block block, uint x, uint y, uint z)
    {
        uint index = x + Size*y + Size*Size*z;
        SetBlock(block, index);
    }
}