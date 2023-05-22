namespace Trilateral.World;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
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
    // Any bigger, and it's possible to overload the 16 bit integer limit for the block IDs.
    // Larger chunks tend to be better, since modern CPU's benefit from working with larger blocks of data at a time.

    //Changing the value is not recomended. I tried to make the code adjust to different chunk sizes appropriately,
    // but it isn't a priority and may or may not be fully supported.
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
    List<Block?>? uidToBlock;
    private Dictionary<string, ushort>? blockToUid;

    public bool IsEmpty()
    {
        if(blockToUid is null)return true;
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
            uidToBlock = null;
            blockToUid = null;
        }
    }
    private ushort GetOrAdd(Block block)
    {
        if(uidToBlock is null || blockToUid is null)
        {
            uidToBlock = new List<Block?>();
            uidToBlock.Add(null);// ID zero is always null.
            blockToUid = new Dictionary<string, ushort>();
            Add(block, 1);
            return 1;
        }
        if(blockToUid.TryGetValue(block.uid, out var id))
        {
            return id;
        }
        id = (ushort)uidToBlock.Count;
        Add(block, id);
        return id;
    }

    private void Add(Block block, ushort id)
    {
        if(uidToBlock is null || blockToUid is null)
        {
            throw new Exception("Yo this ain't supposed to happen");
        }
        uidToBlock.Add(block);
        blockToUid.Add(block.uid, id);
    }
    //creates a new empty chunk
    public Chunk(Vector3i pos)
    {
        this.blocks = null;
        uidToBlock = null;
        blockToUid = null;
        lastChange = DateTime.Now;
        this.pos = pos;
    }

    private Chunk(Block?[] initBlocks, Vector3i pos)
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

    public Chunk(Vector3i pos, Stream streamIn) : this(pos)
    {
        using GZipStream zipStream = new GZipStream(streamIn, CompressionMode.Decompress, true);
        // BinaryReader doesn't really work with GZipStream, because the BinaryReader finds the end of the stream doesn't work.
        // So, we copy the entire thing into a MemoryStream first, since it can easily check if it's at the end or not.
        using MemoryStream stream = new MemoryStream();
        zipStream.CopyTo(stream);
        stream.Seek(0, SeekOrigin.Begin);
        BinaryReader reader = new BinaryReader(stream);
        ChunkSerializationFlag flag = (ChunkSerializationFlag)reader.ReadUInt32();
        if(flag == ChunkSerializationFlag.empty)
        {
            blocks = null;
            uidToBlock = null;
            blockToUid = null;
            return;
        }
        //it's not empty
        if(flag == ChunkSerializationFlag.version_1)
        {
            blocks = new ushort[Length];
            //Read in block data
            for(int i=0; i<Length; ++i)
            {
                var block = reader.ReadUInt16();
                blocks[i] = block;
            }
            //read in block mappings
            uidToBlock = new List<Block?>();
            uidToBlock.Add(null); //0 is always null
            blockToUid = new Dictionary<string, ushort>();
            StringBuilder b = new StringBuilder();
            while(reader.PeekChar() != -1)
            {
                var character = reader.ReadChar();
                if(character == 0)
                {
                    if(!Game.Program.Game.blockRegistry.TryGetValue(b.ToString(), out var block))
                    {
                        //the block ID doesn't exist, so we just use null.
                        block = null;
                    }
                    if(block is not null)Add(block, (ushort)uidToBlock.Count);
                    b = new StringBuilder();
                    continue;
                }
                b.Append(character);
            }
        }
    }
    public Block? GetBlock(uint x, uint y, uint z)
    {
        uint index = y + Size*x + Size*Size*z;
        return GetBlock(index);
    }
    private Block? GetBlock(uint index)
    {
        if(this.blocks == null || uidToBlock == null)
        {
            return null;
        }
        var uid = this.blocks[index];
        return uidToBlock[uid];
    }
    private void SetBlock(Block block, uint index)
    {
        if(this.blocks is null)
        {
            this.blocks = new ushort[Length];
        }
        this.blocks[index] = GetOrAdd(block);
    }
    public void SetBlock(Block block, uint x, uint y, uint z)
    {
        uint index = y + Size*x + Size*Size*z;
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

    enum ChunkSerializationFlag: uint
    {
        empty = 0, //If the chunk is entirely empty
        version_1 = 1,
    }
    //Serialzes the chunk appending it to a stream
    public void SerializeToStream(Stream streamOut)
    {
        using GZipStream stream = new GZipStream(streamOut, CompressionMode.Compress, true);
        //If the chunk is empty, then we simply put the empty flag and be done with it.
        // Since chunks are saved in their own files, we don't need to worry about finding the start and end.
        if(IsEmpty() || blocks is null || uidToBlock is null)
        {
            stream.Write(BitConverter.GetBytes((uint)ChunkSerializationFlag.empty));
            return;
        }
        //It's not empty, so we need to actually serialize it (sad)
        stream.Write(BitConverter.GetBytes((uint)ChunkSerializationFlag.version_1));
        //First, serialize the entire chunk.
        // It will always be the same length (assuming the chunk size doesn't change) so that is no worry.
        foreach(uint value in blocks)
        {
            stream.Write(BitConverter.GetBytes((short)value));
        }
        //Next, the number to block mapping.
        // We map a blocks numerical ID within this chunk to its text ID.
        for(ushort i=0; i<uidToBlock.Count; ++i)
        {
            var block = uidToBlock[i];
            string id;
            if(block is null){
                id = "trilateral:none";
            }
            else 
            {
                id = block.uid;
            }
            // write a null-terminated string id.
            stream.Write(Encoding.ASCII.GetBytes(id));
            stream.WriteByte((byte)0);
        }
    }
}