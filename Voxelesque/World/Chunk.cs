namespace Voxelesque.World;

using System;
using System.Collections.Generic;

//This class uses a few optimizations to make things nice and memory efficient without sacrificing too much speed.
// Memory optimizations:
// -Pointers are huge. 8 bytes each. Block is a refernce type. By assigning each block and ID, we can cut that down to 2 bytes each.
//  It's well worth the tiny bit of extra memory to store the mapping from ID to Block
//  1.6gb -> 0.7gb (roughly half)
// 
public class Chunk
{
    // Chunks are quite unusually shaped (geometrically speaking), but the data is stored as a cube.
    public const ushort Size = 32;
    //The chunk length is the total number of blocks in a chunk
    const uint Length = Size*Size*Size;

    //when the chunk was last modified
    DateTime lastChange;
    public DateTime LastChange{get=>lastChange;}

    private ushort[]? blocks;
    List<Block?>? idToBlock;
    private Dictionary<string, ushort>? blockToId;


    private ushort GetOrAdd(Block block)
    {
        if(idToBlock is null || blockToId is null)
        {
            idToBlock = new List<Block?>();
            idToBlock.Add(null);// ID zero is always null.
            blockToId = new Dictionary<string, ushort>();
            Add(block, 1);
            return 1;
        }
        if(blockToId.TryGetValue(block.name, out var id))
        {
            return id;
        }
        id = (ushort)idToBlock.Count;
        Add(block, id);
        return id;
    }

    private void Add(Block block, ushort id)
    {
        if(idToBlock is null || blockToId is null)
        {
            throw new Exception("Yo this ain't supposed to happen");
        }
        idToBlock.Add(block);
        blockToId.Add(block.name, id);
    }
    //creates a new empty chunk
    public Chunk()
    {
        this.blocks = null;
        idToBlock = null;
        blockToId = null;
        lastChange = DateTime.Now;
    }

    public Chunk(Block?[] initBlocks)
    {
        if(initBlocks.Length != Length)
        {
            throw new Exception("Cannot create a chunk with the incorrect length!");
        }
        this.blocks = new ushort[Length];
        for(uint index = 0; index<Length; index++)
        {
            Block? block = initBlocks[index];
            ushort id;
            if(block is null)
            {
                id = 0;
                continue;
            }
            id = GetOrAdd(block);
            this.blocks[index] = id;

        }
        lastChange = DateTime.Now;
    }
    public Block? GetBlock(uint x, uint y, uint z)
    {
        uint index = x + Size*y + Size*Size*z;
        return GetBlock(index);
    }
    public Block? GetBlock(uint index)
    {
        if(this.blocks == null || idToBlock == null)
        {
            return null;
        }
        return idToBlock[this.blocks[index]];
    }
    public void SetBlock(Block block, uint index)
    {
        if(this.blocks is null)
        {
            this.blocks = new ushort[Length];
        }
        this.blocks[index] = GetOrAdd(block);
    }
    public void SetBlock(Block block, uint x, uint y, uint z)
    {
        uint index = x + Size*y + Size*Size*z;
        SetBlock(block, index);
    }

    public void UpdateLastChange()
    {
        lastChange = DateTime.Now;
    }
}