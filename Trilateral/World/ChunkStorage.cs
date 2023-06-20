namespace Trilateral.World;

using OpenTK.Mathematics;
using System.IO;
using System;
using System.Collections.Generic;
using Trilateral.Utility;
sealed class ChunkStorage
{
    private string pathToSaveFolder;
    private Dictionary<Vector3i, ChunkSection> sections;
    public ChunkStorage(string pathToSaveFolder)
    {
        this.pathToSaveFolder = pathToSaveFolder;
        if(!Directory.Exists(pathToSaveFolder))
        {
            Directory.CreateDirectory(pathToSaveFolder);
        }
        if(!Directory.Exists(pathToSaveFolder + "/chunks/"))
        {
            Directory.CreateDirectory(pathToSaveFolder + "/chunks/");
        }
        sections = new Dictionary<Vector3i, ChunkSection>();
    }
    public void SaveChunk(Chunk chunk)
    {
        Profiler.PushRaw("SaveChunk");
        try{
            Vector3i section = MathBits.DivideFloor(chunk.pos, ChunkSection.Size);
            //See if we already have a section
            ChunkSection? chunkSection;
            lock(sections)
            {
                if(!sections.TryGetValue(section, out chunkSection))
                {
                    //If the section isn't already initialized, then initialize it.
                    chunkSection = new ChunkSection(pathToSaveFolder + "/chunks/" + section.ToString() + ".tws");
                    sections.Add(section, chunkSection);
                }
            }
            chunkSection.SaveChunk(chunk);
        } catch(Exception e)
        {
            System.Console.Error.WriteLine("Error saving chunk " + chunk.pos + ": " + e.Message + "\nStacktrace:" + e.StackTrace);
        }
        Profiler.PopRaw("SaveChunk");
        
    }
    public Chunk? LoadChunk(Vector3i pos)
    {
        Profiler.PushRaw("LoadChunk");
        try{
            Vector3i section = MathBits.DivideFloor(pos, ChunkSection.Size);
            ChunkSection? chunkSection;
            lock(sections)
            {
                if(!sections.TryGetValue(section, out chunkSection))
                {
                    //If the section isn't already initialized, then initialize it.
                    chunkSection = new ChunkSection(pathToSaveFolder + "/chunks/" + section.ToString() + ".tws");
                    sections.Add(section, chunkSection);
                }
            }
            Profiler.PopRaw("LoadChunk");
            return chunkSection.LoadChunk(pos);
        } catch(Exception e)
        {
            System.Console.Error.WriteLine("Error loading chunk " + pos + ": " + e.Message + "\nStacktrace:" + e.StackTrace);
            Profiler.PopRaw("LoadChunk");
            return null;
        }
        
    }

    public int NumberOfCachedSections
    {
        get => sections.Count;
    }

    public void Flush()
    {
        System.Console.WriteLine("Flushing chunk storage");
        lock(sections)
        {
            foreach(var section in sections)
            {
                section.Value.Dispose();
            }
            sections.Clear();
        }
    }
}