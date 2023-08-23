namespace Trilateral.World;

using OpenTK.Mathematics;
using System.IO;
using System;
using System.Collections.Generic;
using Trilateral.Utility;
using System.Threading.Tasks;

sealed class ChunkStorage
{
    private readonly string pathToSaveFolder;
    private readonly Dictionary<Vector3i, ChunkSection> sections;
    // Save chunks periodically rather than whenever they change
    private HashSet<Chunk> chunksToSave;
    // A second list that is swapped with the main one
    // when a flush starts, so the game can keep running
    // while the world is being saved in the background.
    private HashSet<Chunk> otherChunksToSave;
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
        chunksToSave = new HashSet<Chunk>();
        otherChunksToSave = new HashSet<Chunk>();
    }
    public void SaveChunk(Chunk chunk)
    {
        lock(chunksToSave)
        {
            //TODO: maybe find a better way to replace pre-existing chunks.
            if(chunksToSave.Contains(chunk))
            {
                chunksToSave.Remove(chunk);
            }
            chunksToSave.Add(chunk);
        }
    }

    private void SaveChunkForReal(Chunk chunk)
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

    private bool Flushing = false;

    public async void Flush()
    {
        if(Flushing){
            System.Console.WriteLine("Chunk flush is taking longer than " + Program.Game.Settings.chunkFlushPeriod + "; flush attempted to occur twice concurrently");
            return;
        }
        Flushing = true;
        System.Console.WriteLine("Started flushing chunk storage");
        //swap chunksToSave and otherChunksToSave
        lock(chunksToSave)
        {
            (chunksToSave, otherChunksToSave) = (otherChunksToSave, chunksToSave);
        }
        await Parallel.ForEachAsync(otherChunksToSave, (chunk, _) =>         {
            // It is worth noting that because chunks are references,
            // It is possible for a chunk to be modified while it's being serialized.
            // However, I doubt that will be an issue.
            SaveChunkForReal(chunk);
            return ValueTask.CompletedTask;
        });
        otherChunksToSave.Clear();
        lock(sections)
        {
            foreach(var section in sections)
            {
                section.Value.Dispose();
            }
            sections.Clear();
        }
        Flushing = false;
        System.Console.WriteLine("Finished flushing chunk storage");
    }
}