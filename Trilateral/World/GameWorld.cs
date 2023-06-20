using System;
using Trilateral.World;

public sealed class GameWorld : IDisposable
{
    string pathToSaveFolder;
    public readonly ChunkManager chunkManager;
    
    public GameWorld(string pathToSaveFolder, IChunkGenerator generator, float renderThreadsMultiplier, float worldThreadsMultiplier)
    {
        chunkManager = new ChunkManager(generator, pathToSaveFolder, renderThreadsMultiplier, worldThreadsMultiplier);
        this.pathToSaveFolder = pathToSaveFolder;
    }
    
    public void Dispose()
    {
        chunkManager.Dispose();
    }
}