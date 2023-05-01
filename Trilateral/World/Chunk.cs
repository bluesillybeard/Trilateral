namespace Trilateral.World;

using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

//This class uses a few optimizations to make things nice and memory efficient without sacrificing too much speed.
// Memory optimizations:
// -Pointers are huge. 8 bytes each. Block is a refernce type. By assigning each block and ID, we can cut that down to 2 bytes each.
//  It's well worth the tiny bit of extra memory to store the mapping from ID to Block
//  1.6gb -> 0.7gb (roughly half)
// 
public class Chunk
{
    // Chunks are quite unusually shaped (geometrically speaking), but the data is stored as a cube.
    // I chose 40x40x40 because it is the largest size that allows my memory optimization to work.
    // Larger chunks tend to be better, since modern CPU's benefit from working with larger blocks of data at a time.
    public const ushort Size = 40;
    // TODO: In the future I want chunks to be larger in the vertical direction, like 16x16x256, since the world generation works one column at a time,
    // And larger columns means the CPU works in larger blocks. However, that would require a lot of redesigning, which I really don't want to do right now.

    //The chunk length is the total number of blocks in a chunk
    const uint Length = Size*Size*Size;

    //when the chunk was last modified
    DateTime lastChange;
    public DateTime LastChange{get=>lastChange;}
    public Vector3i pos;

    private ushort[]? blocks;
    List<Block?>? idToBlock;
    private Dictionary<string, ushort>? blockToId;

    public bool IsEmpty()
    {
        if(blockToId is null)return true;
        if(blocks is null)return true;
        foreach(uint block in blocks)
        {
            if(block != 0)return false;
        }
        return true;
    }

    public void Optimize()
    {
        if(this.IsEmpty())
        {
            blocks = null;
            idToBlock = null;
            blockToId = null;
        }
    }
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
    public Chunk(Vector3i pos)
    {
        this.blocks = null;
        idToBlock = null;
        blockToId = null;
        lastChange = DateTime.Now;
        this.pos = pos;
    }

    public Chunk(Block?[] initBlocks, Vector3i pos)
    {
        this.pos = pos;
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
    /**
    <summary>
    Notifies that this chunk has been changed.
    It would automatically do this, but DateTime.Now
    has a very large performance peanalty when called 
    for every single modification, so this method
    allows for a large set of blocks to be modified
    without repeadetly getting the current time.
    </summary>
    */
    public void UpdateLastChange()
    {
        lastChange = DateTime.Now;
    }
}