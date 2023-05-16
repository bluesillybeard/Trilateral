namespace Trilateral.World;

using OpenTK.Mathematics;
using System.IO;
using System;
using System.IO.Compression;
using System.Collections.Generic;
using Trilateral.Utility;
sealed class ChunkStorage
{
    private string pathToSaveFolder;
    private Dictionary<Vector3i, (ZipArchive archive, FileStream file)> archives;
    private Dictionary<Vector3i, (ZipArchive archive, FileStream file)> otherArchives;
    public ChunkStorage(string pathToSaveFolder)
    {
        this.pathToSaveFolder = pathToSaveFolder;
        if(!Directory.Exists(pathToSaveFolder))
        {
            Directory.CreateDirectory(pathToSaveFolder);
            Directory.CreateDirectory(pathToSaveFolder + "/chunks/");
        }
        archives = new Dictionary<Vector3i, (ZipArchive, FileStream)>();
        otherArchives = new Dictionary<Vector3i, (ZipArchive archive, FileStream file)>();
    }
    public void SaveChunk(Chunk chunk)
    {
        Vector3i section = MathBits.DivideFloor(chunk.pos, 32);
        var pos = chunk.pos;
        string path = pathToSaveFolder + "/chunks/" + section.X + "_" + section.Y + "_" + section.Z + ".zip";
        string name = pos.X + "_" + pos.Y + "_" + pos.Z + ".vchunk";
        lock(archives)
        {
            //First, see if the archive is already open
            if(!archives.TryGetValue(section, out var archive))
            {
                //The archive isn't open yet.
                if(File.Exists(path))
                {
                    //If there is already an existing file, open it.
                    FileStream file = new FileStream(path, FileMode.Open);
                    ZipArchive zip = new ZipArchive(file, ZipArchiveMode.Update, true);
                    archive = (zip, file);
                    archives.Add(section, archive);
                }
                else 
                {
                    //If there isn't a file, make a new one
                    FileStream file = new FileStream(path, FileMode.Create);
                    //create a new archive. Except I can only write to it.
                    // Why isn't there an option to be able to create a new archive that I can also read from? No idea.
                    ZipArchive zip = new ZipArchive(file, ZipArchiveMode.Create, true);
                    // TO counteract that, I just close the archive and open it again in Update mode.
                    zip.Dispose();
                    zip = new ZipArchive(file, ZipArchiveMode.Update, true);
                    //TODO: fix archive not being readable after creation
                    archive = (zip, file);
                    archives.Add(section, archive);
                }
                
            }
            var entry = archive.archive.GetEntry(name);
            if(entry is not null)entry.Delete();
            entry = archive.archive.CreateEntry(name);
            var stream = entry.Open();
            chunk.SerializeToStream(stream);
            stream.Dispose();
        }
        //chunk.SerializeToStream(file);
        //file.Dispose();
    }
    public Chunk? LoadChunk(Vector3i pos)
    {
        try{
            Vector3i section = MathBits.DivideFloor(pos, 32);
            string path = pathToSaveFolder + "/chunks/" + section.X + "_" + section.Y + "_" + section.Z + ".zip";
            string name = pos.X + "_" + pos.Y + "_" + pos.Z + ".vchunk";
            lock(archives)
            {
                //First, see if the archive is already open
                if(!archives.TryGetValue(section, out var archive))
                {
                    //The archive isn't open yet.
                    if(!File.Exists(path))
                    {
                        return null;
                    }
                    //If there is already an existing file, open it.
                    FileStream file = new FileStream(path, FileMode.Open);
                    archive = (new ZipArchive(file, ZipArchiveMode.Update, true), file);
                    archives.Add(section, archive);
                }
                var entry = archive.archive.GetEntry(name);
                if(entry is null)
                {
                    return null;
                }
                var stream = entry.Open();
                if(stream.Length < 4)
                {
                    System.Console.Error.WriteLine("Chunk entry was empty!");
                    stream.Dispose();
                    entry.Delete();
                    return null;
                }
                var chunk = new Chunk(pos, stream);
                stream.Dispose();
                return chunk;
            }
        } catch (Exception e)
        {
            System.Console.Error.WriteLine("Error loading chunk: " + e.Message + "\nStacktrace:" + e.StackTrace);
            return null;
        }
        
    }

    public int NumberOfCachedSections
    {
        get => archives.Count;
    }

    public void Flush()
    {
        //Here's the deal: flushing takes a very long time.
        // So, instead of locking the archives for ages and focing the entire game to freeze,
        // we sneakily swap out the data structure when nobody is looking.
        lock(archives)
        {
            lock(otherArchives)
            {
                var temp = archives;
                archives = otherArchives;
                otherArchives = temp;
            }
            
        }
        foreach(var section in otherArchives)
        {
            var archive = section.Value;
            var zip = archive.archive;
            var file = archive.file;
            zip.Dispose();
            file.Dispose();
        }
        otherArchives.Clear();
    }
}