namespace Trilateral.World;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;

using OpenTK.Mathematics;

using Utility;
struct ChunkEntry
{
    public ChunkEntry()
    {
        offset = 0;
        length = 0;
    }
    public uint offset;
    public uint length;
}
/*
Originally, I tried to use memory mapped files, but that became too complicated to implement for my little brain to handle.
TODO: if saving/loading chunks is too slow, look at MMFs again and see if there is a significant performance improvement
*/
public sealed class ChunkSection : IDisposable
{
    public const int Size = 16;
    public const int Length = Size*Size*Size;

    private string filePath;
    private ChunkEntry[] entries;
    private FileStream file;

    public ChunkSection(string filePath)
    {
        this.filePath = filePath;
        file = new FileStream(filePath, FileMode.OpenOrCreate);
        entries = new ChunkEntry[Length];
        if(file.Length >= Length * 8)
        {
            BinaryReader r = new BinaryReader(file);
            for(int index=0; index<Length; index++)
            {
                ChunkEntry entry = new ChunkEntry();
                entry.offset = r.ReadUInt32();
                entry.length = r.ReadUInt32();
                entries[index] = entry;
            }
        }
        else 
        {
            BinaryWriter w = new BinaryWriter(file);
            for(int index=0; index<Length; index++)
            {
                ChunkEntry entry = new ChunkEntry();
                entry.offset = 0;
                entry.length = 0;
                w.Write((uint)0);
                w.Write((uint)0);
            }
        }
        
    }

    public void SaveChunk(Chunk c)
    {
        Vector3i pos = MathBits.Mod(c.pos, Size);
        int index = GetIndex(pos);
        //Serialize the chunk into RAM
        MemoryStream stream = new MemoryStream();
        c.SerializeToStream(stream);
        //See if it will fit where the chunk already is
        stream.Seek(0, SeekOrigin.Begin);
        if(entries[index].length >= stream.Length)
        {
            //if it fits, write it.
            lock(file)
            {
                file.Seek(entries[index].offset, SeekOrigin.Begin);
                stream.CopyTo(file);
                entries[index].length = (uint)stream.Length;
                return;
            }
        }
        //If it doesn't fit, find a place where it does
        var freeChunkOffset = FindFreeZone((uint)stream.Length);
        lock(file)
        {
            file.Seek(freeChunkOffset, SeekOrigin.Begin);
            //Thankfully, FileStream is more than happy to expand the file for us.
            stream.CopyTo(file);
            entries[index].offset = freeChunkOffset;
            entries[index].length = (uint)stream.Length;
            return;
        }
    }

    public Chunk? LoadChunk(Vector3i absolutePos)
    {
        Vector3i pos = MathBits.Mod(absolutePos, Size);
        int index = GetIndex(pos);
        //If the entry is null, there is no chunk to load
        ChunkEntry entry = entries[index];
        if(entry.offset == 0) return null;
        lock(file)
        {
            file.Seek(entry.offset, SeekOrigin.Begin);
            return new Chunk(absolutePos, file);
        }
    }

    public void Dispose()
    {
        lock(file)
        {
            file.Seek(0, SeekOrigin.Begin);
            BinaryWriter w = new BinaryWriter(file);
            for(int index=0; index<Length; index++)
            {
                ChunkEntry entry = entries[index];
                w.Write(entry.offset);
                w.Write(entry.length);
            }
        }
        file.Dispose();

    }

    private int GetIndex(int x, int y, int z)
    {
        return x + y*Size + z*Size*Size;
    }
    private int GetIndex(Vector3i pos)
    {
        return pos.X + pos.Y*Size + pos.Z*Size*Size;
    }

    private uint FindFreeZone(uint length)
    {
        uint gapLength = 0;
        for(uint offset = Length * 8; ; offset++)
        {
            if(gapLength >= length)
            {
                return offset - gapLength;
            }
            gapLength++;
            if(IsOccupied(offset))
            {
                gapLength = 0;
            }
        }
        throw new Exception("could not find a free zone. Should never happen under any circumstance!");
    }

    private bool IsOccupied(uint offset)
    {
        foreach(ChunkEntry e in entries)
        {
            if(e.offset <= offset && (e.offset + e.length) >= offset) return true;
        }
        return false;
    }
}