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
public sealed class Chunk
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
    public Vector3i pos;
    private ushort[]? blocks;
    List<IBlock>? uidToBlock;
    private readonly IBlock fill;
    private Dictionary<string, ushort>? blockToUid;
    public bool IsFill()
    {
        if(blockToUid is null)return true;
        if(uidToBlock is null)return true;
        if(blocks is null)return true;
        foreach(ushort block in blocks)
        {
            if(uidToBlock[block] != fill)
            {
                return false;
            }
        }
        return true;
    }

    public void Optimize()
    {
        if(this.IsFill())
        {
            blocks = null;
            uidToBlock = null;
            blockToUid = null;
        }
    }
    private ushort GetOrAdd(IBlock block)
    {
        if(uidToBlock is null || blockToUid is null)
        {
            uidToBlock = new List<IBlock>();
            blockToUid = new Dictionary<string, ushort>();
        }
        if(blockToUid.TryGetValue(block.UUID, out var id))
        {
            return id;
        }
        return Add(block);
    }

    private ushort Add(IBlock block)
    {
        if(uidToBlock is null || blockToUid is null)
        {
            uidToBlock = new List<IBlock>();
            blockToUid = new Dictionary<string, ushort>();
        }
        var id = (ushort)uidToBlock.Count;
        if(blockToUid.TryAdd(block.UUID, id))
        {
            uidToBlock.Add(block);
            return id;
        }
        else
        {
            Console.WriteLine("WARNING: Tried to add duplicate blockToUid mapping for block \"" + block.Name + "\" in chunk " + pos);
            return 0;
        }
    }
    //creates a new empty chunk
    public Chunk(Vector3i pos, IBlock fill)
    {
        this.blocks = null;
        uidToBlock = null;
        blockToUid = null;
        this.pos = pos;
        this.fill = fill;
    }

    public Chunk(Vector3i pos, Stream streamIn)
    {
        this.pos = pos;
        using GZipStream zipStream = new(streamIn, CompressionMode.Decompress, true);
        // BinaryReader doesn't really work with GZipStream, because the BinaryReader finds the end of the stream doesn't work.
        // So, we copy the entire thing into a MemoryStream first, since it can easily check if it's at the end or not.
        using MemoryStream stream = new();
        zipStream.CopyTo(stream);
        stream.Seek(0, SeekOrigin.Begin);
        BinaryReader reader = new(stream);
        ChunkSerializationFlag flag = (ChunkSerializationFlag)reader.ReadUInt32();
        if(flag == ChunkSerializationFlag.filled)
        {
            //The chunk is just a singular block "mapping"
            StringBuilder b = new();
            while(reader.PeekChar() != -1 && reader.PeekChar() != 0)
            {
                var character = reader.ReadChar();
                b.Append(character);
            }
            string blockid = b.ToString();
            if(!Program.Game.BlockRegistry.TryGetValue(blockid, out var block))
            {
                //the block ID doesn't exist, so we just use void
                block = Program.Game.AirBlock;
                System.Console.WriteLine("WARNING: block id \'" + blockid + "\' does not exist in the block registry");
            }
            fill = block;
            return;
        }
        //it's not empty
        if(flag == ChunkSerializationFlag.version_1)
        {
            blocks = new ushort[Length];
            //Read in block data
            for(int i=0; i<Length; ++i)
            {
                blocks[i] = reader.ReadUInt16();
            }
            //read in block mappings
            uidToBlock = new List<IBlock>();
            blockToUid = new Dictionary<string, ushort>();
            StringBuilder b = new();
            while(reader.PeekChar() != -1)
            {
                var character = reader.ReadChar();
                if(character == 0)
                {
                    if(!Program.Game.BlockRegistry.TryGetValue(b.ToString(), out var block))
                    {
                        //the block ID doesn't exist, so we just use void
                        block = Program.Game.AirBlock;
                        System.Console.WriteLine("WARNING: block id \'" + b.ToString() + "\' does not exist in the block registry. Chunk " + pos);
                    }
                    Add(block);
                    //This is mostly useless, but fill is non-nullable and i need to keep the compiler happy.
                    fill = block;
                    b = new StringBuilder();
                    continue;
                }
                b.Append(character);
            }
            if(fill is null)throw new Exception("Invalid chunk " + pos + "has no block mappings");
            return;
        }
        throw new Exception("Invalid chunk type " + flag + "in chunk " + pos);
    }
    public IBlock GetBlock(uint x, uint y, uint z)
    {
        uint index = y + Size*x + Size*Size*z;
        return GetBlock(index);
    }
    private IBlock GetBlock(uint index)
    {
        if(this.blocks == null || uidToBlock == null)
        {
            return fill;
        }
        var uid = this.blocks[index];
        return uidToBlock[uid];
    }
    private void SetBlock(IBlock block, uint index)
    {
        if(this.blocks is null)
        {
            this.blocks = new ushort[Length];
            Array.Fill<ushort>(this.blocks, GetOrAdd(fill));
        }
        this.blocks[index] = GetOrAdd(block);
    }
    public void SetBlock(IBlock block, uint x, uint y, uint z)
    {
        uint index = y + Size*x + Size*Size*z;
        SetBlock(block, index);
    }

    enum ChunkSerializationFlag: uint
    {
        filled = 0, //If the chunk is entirely one block
        version_1 = 1,
    }
    //Serialzes the chunk appending it to a stream
    public void SerializeToStream(Stream streamOut)
    {
        using GZipStream stream = new(streamOut, CompressionMode.Compress, true);
        //filled chunks are basically just a single block mapping
        if(IsFill() || blocks is null || uidToBlock is null)
        {
            stream.Write(BitConverter.GetBytes((uint)ChunkSerializationFlag.filled));
            stream.Write(Encoding.ASCII.GetBytes(fill.UUID));
            stream.WriteByte(0);
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
                Console.WriteLine("WARNING: Replaced block with local id " + i + " with 'void'");
                id = "trilateral:void";
            }
            else
            {
                id = block.UUID;
            }
            // write a null-terminated string id.
            stream.Write(Encoding.ASCII.GetBytes(id));
            stream.WriteByte(0);
        }
    }
}