namespace Trilateral.World;

using System;
using System.Diagnostics;
using System.IO;

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
    public const int NumberOfEntries = Size*Size*Size;

    private string filePath;
    private ChunkEntry[] entries;
    private FileStream file;

    public ChunkSection(string filePath)
    {
        this.filePath = filePath;
        file = new FileStream(filePath, FileMode.OpenOrCreate);
        entries = new ChunkEntry[NumberOfEntries];
        if(file.Length >= NumberOfEntries * 8)
        {
            BinaryReader r = new BinaryReader(file);
            for(int index=0; index<NumberOfEntries; index++)
            {
                ChunkEntry entry = new ChunkEntry();
                entry.offset = r.ReadUInt32();
                entry.length = r.ReadUInt32();
                entries[index] = entry;
            }
            //Check for any overlaps
            for(int first=0; first<entries.Length; first++)
            {
                for(int second = first+1; second < entries.Length; second++)
                {
                    ChunkEntry firstEntry = entries[first];
                    ChunkEntry secondEntry = entries[second];
                    if(firstEntry.offset == 0 || secondEntry.offset == 0)continue;
                    if(firstEntry.offset <= secondEntry.offset && (firstEntry.offset + firstEntry.length) > secondEntry.offset
                    || secondEntry.offset <= firstEntry.offset && (secondEntry.offset + secondEntry.length) > firstEntry.offset)
                    {
                        System.Console.Error.WriteLine("Chunk Collision in section \"" + filePath + "\"");
                        System.Console.Error.WriteLine("\tentries #" + first + " (" + firstEntry.offset + " " + firstEntry.length + ") & #" + second + " (" + secondEntry.offset + " " + secondEntry.length + ")");
                        
                    }
                }
            }
        }
        else 
        {
            BinaryWriter w = new BinaryWriter(file);
            for(int index=0; index<NumberOfEntries; index++)
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
        using var _ = Profiler.Push("SaveChunkInSection");
        lock(file)
        {
            Vector3i pos = MathBits.Mod(c.pos, Size);
            int index = GetIndex(pos);
            //Serialize the chunk into RAM
            MemoryStream stream = new MemoryStream(1024);
            Profiler.PushRaw("Serialize");
            c.SerializeToStream(stream);
            stream.Seek(0, SeekOrigin.Begin);
            Profiler.PopRaw("Serialize");
            //See if it will fit where the chunk already is
            if(entries[index].length >= stream.Length)
            {
                Profiler.PushRaw("WriteToFile");
                //if it fits, write it.
                file.Seek(entries[index].offset, SeekOrigin.Begin);
                stream.CopyTo(file);
                entries[index].length = (uint)stream.Length;
                Profiler.PopRaw("WriteToFile");
                return;
            }
            //If it doesn't fit, find a place where it does
            Profiler.PushRaw("FindFreeZone");
            //Set the current entry as empty, so it's allowed to override the data of the entry its replacing
            ChunkEntry oldEntry = entries[index];
            entries[index] = new ChunkEntry();
            var freeChunkOffset = FindFreeZone((uint)stream.Length);
            Profiler.PopRaw("FindFreeZone");
            // //Write 0xFF into the previous location, so it's more clear where the unused area is.
            // file.Seek(oldEntry.offset, SeekOrigin.Begin);
            // for(int i=0; i<oldEntry.length; i++)
            // {
            //     file.WriteByte(0xFF);
            // }
            //Thankfully, FileStream is more than happy to expand the file for us.
            Profiler.PushRaw("WriteToFile");
            file.Seek(freeChunkOffset, SeekOrigin.Begin);
            stream.CopyTo(file);
            Profiler.PopRaw("WriteToFile");
            entries[index].offset = freeChunkOffset;
            entries[index].length = (uint)stream.Length;
            return;
        }
    }

    public Chunk? LoadChunk(Vector3i absolutePos)
    {
        using var _ = Profiler.Push("LoadChunkInSection");
        lock(file)
        {
            Vector3i pos = MathBits.Mod(absolutePos, Size);
            int index = GetIndex(pos);
            //If the entry is null, there is no chunk to load
            ChunkEntry entry = entries[index];
            if(entry.offset == 0) return null;
            file.Seek(entry.offset, SeekOrigin.Begin);
            //TODO (not important in the slightest): fix the length problem in a way that doesn't involve copying it to another stream
            // Ideally, just find a different compression format that can actually figure out where the end is.
            byte[] chunkData = new byte[entry.length];
            file.ReadExactly(chunkData, 0, (int)entry.length);
            using MemoryStream chunkDataStream = new MemoryStream(chunkData);
            return new Chunk(absolutePos, chunkDataStream);
        }
        
    }

    public void Dispose()
    {
        lock(file)
        {
            file.Seek(0, SeekOrigin.Begin);
            BinaryWriter w = new BinaryWriter(file);
            for(int index=0; index<NumberOfEntries; index++)
            {
                ChunkEntry entry = entries[index];
                w.Write(entry.offset);
                w.Write(entry.length);
            }
            w.Dispose();
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
        //sort entries by their offset
        Span<ChunkEntry> sortedEntries = stackalloc ChunkEntry[NumberOfEntries];
        ((Span<ChunkEntry>)entries).CopyTo(sortedEntries);
        sortedEntries.Sort((x, y) => {return (int)x.offset - (int)y.offset;});
        //skip past all of the entries that aren't allocated
        int i=0;
        while(i<sortedEntries.Length && sortedEntries[i].offset == 0)
        {
            i++;
        }
        sortedEntries = sortedEntries.Slice(i);
        if(sortedEntries.Length == 0)
        {
            //All of the entries are empty, so just return the start of the file's data area.
            // I suppose it could be called the heap (of the file)
            return NumberOfEntries*sizeof(uint)*2;
        }
        //Now find an empty spot, if there is one.
        i=1;
        while(i<sortedEntries.Length)
        {
            //calculate the gap between index i and index i-1
            uint gapStart = sortedEntries[i-1].offset+sortedEntries[i-1].length;
            uint gapEnd = sortedEntries[i].offset;
            if(gapStart > gapEnd)
            {
                //This shouldn't happen
                System.Console.Error.WriteLine("Invalid chunk section \"" + filePath + "\" : Entries overlap");
                continue;
            }
            uint gap = gapEnd - gapStart;
            if(gap >= length)return gapStart;
            i++;
        }
        //If there were no gaps big enough, then we just return the end of the file, since the file stream will automatically expand it
        return sortedEntries[sortedEntries.Length-1].offset + sortedEntries[sortedEntries.Length-1].length;
    }

    private int GetOccupierIndex(uint offset)
    {
        for(int index = 0; index < NumberOfEntries; index++)
        {
            var e = entries[index];
            if(e.offset <= offset && (e.offset + e.length) >= offset) return index;
        }
        return -1;
    }
}