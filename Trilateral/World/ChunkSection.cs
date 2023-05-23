namespace Trilateral.World;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;

using OpenTK.Mathematics;

using Utility;
struct ChunkEntry
{
    public uint offset;
    public uint length;
}

public sealed class ChunkSection : IDisposable
{
    public const int Size = 16;
    public const int Length = Size*Size*Size;
    //We use a memory mapped file, to avoid doing things with the entire file at once.
    // However, that presents a problem of allocating areas for different chunks.
    // The format allows for chunks to be placed in any order in any location,
    // So a fairly simple allocation system works just fine.
    private ChunkEntry[] chunkPointerTable;
    private MemoryMappedFile file;
    private Dictionary<Vector3i, MemoryMappedViewStream> chunkViews;

    public ChunkSection(string filePath)
    {
        chunkPointerTable = new ChunkEntry[Length];
        //TODO: don't make a bajillion syscalls just to make a new file
        if(!File.Exists(filePath))
        {
            File.Create(filePath);
        }
        file = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, null, Length * 8);
        chunkViews = new Dictionary<Vector3i, MemoryMappedViewStream>();
    }

    private static int GetIndex(int x, int y, int z)
    {
        return x + Size * y + Size*Size * z;
    }

    private bool IsOccupied(int offset)
    {
        foreach(ChunkEntry chunk in chunkPointerTable)
        {
            //If the offset is within an existing chunk, then it's occupied.
            if(chunk.offset <= offset && (chunk.length + chunk.offset) >= offset)
            {
                return true;
            }
        }
        return false;
    }

    private MemoryMappedViewStream GetOrCreateView(Vector3i id)
    {
        var index = GetIndex(id.X, id.Y, id.Z);
        //First, see if our view is already open
        if(!chunkViews.TryGetValue(id, out var view))
        {
            //If there isn't an open view for the chunk, make a new one.
            var entry = chunkPointerTable[index];
            view = file.CreateViewStream(entry.offset, entry.length);
            chunkViews.Add(id, view);
        }
        return view;
    }
    public Chunk LoadChunk(Vector3i pos)
    {
        Vector3i id = MathBits.Mod(pos, Size);
        var index = GetIndex(id.X, id.Y, id.Z);
        //First, see if our view is already open
        var view = GetOrCreateView(id);
        //Now that the view is open, load the chunk out of it.
        var chunk = new Chunk(pos, view);
        //Seek it back to the start since most methods expect the stream to be at the beginning
        view.Seek(0, SeekOrigin.Begin);
        return chunk;
    }

    private Stream FindViewForChunkSave(Vector3i id, long length)
    {
        var index = GetIndex(id.X, id.Y, id.Z);
        //Find a place where it will fit
        // First, see if it will fit where the chunk used to be.
        if(chunkPointerTable[index].length >= length)
        {
            return GetOrCreateView(id);
        }
        //If it doesn't fit where the chunk used to be, then find an empty space large enough
        uint gapLength = 0;
        int offset;
        for(offset = 0; gapLength >= length; offset++)
        {
            if(IsOccupied(offset))
            {
                gapLength = 0;
            }
            gapLength++;
        }
        chunkViews[id].Dispose();
        chunkViews.Remove(id);
        var view = file.CreateViewStream(offset, length);
        chunkViews.Add(id, view);
        return view;
    }
    public void SaveChunk(Chunk c)
    {
        Vector3i id = MathBits.Mod(c.pos, Size);
        var index = GetIndex(id.X, id.Y, id.Z);
        //Serialize the chunk into a MemoryStream.
        using MemoryStream stream = new MemoryStream();
        c.SerializeToStream(stream);
        stream.Seek(0, SeekOrigin.Begin);
        var output = FindViewForChunkSave(id, stream.Length);
        stream.CopyTo(output);

    }
    public void Dispose()
    {
        foreach(var view in chunkViews)
        {
            view.Value.Dispose();
        }
        file.Dispose();
    }
}