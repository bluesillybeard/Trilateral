namespace Trilateral.World;

using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System;

using OpenTK.Mathematics;

using Utility;
using VRenderLib.Utility;
using VRenderLib.Threading;
using Trilateral;
using Trilateral.World.ChunkGenerators;

public sealed class ChunkManager
{
    private readonly ConcurrentDictionary<Vector3i, Chunk> chunks;
    public readonly ChunkRenderer renderer;
    private readonly IChunkGenerator generator;
    private readonly LocalThreadPool pool;
    //TODO: Figure out the best data structure for this.
    private readonly HashSet<Vector3i> chunksBeingLoaded; 
    private readonly List<Chunk> chunksFinishedLoading;
    private readonly HashSet<Chunk> modifiedChunks;
    
    private readonly ChunkStorage storage;
    public ChunkManager(IChunkGenerator generator, string pathToSaveFolder, float renderThreadsMultiplier, float worldThreadsMultiplier)
    {
        this.generator = generator;
        chunks = new ConcurrentDictionary<Vector3i, Chunk>();//new Dictionary<Vector3i, Chunk>();
        renderer = new ChunkRenderer(renderThreadsMultiplier);
        pool = new LocalThreadPool(int.Max(1, (int)(Environment.ProcessorCount*worldThreadsMultiplier)));
        chunksBeingLoaded = new HashSet<Vector3i>();
        chunksFinishedLoading = new List<Chunk>();
        storage = new ChunkStorage(pathToSaveFolder);
        modifiedChunks = new HashSet<Chunk>();
    }
    private Chunk LoadChunk(Vector3i pos, out bool generatedNew)
    {
        var chunk = storage.LoadChunk(pos);
        generatedNew = false;
        if(chunk is not null)return chunk;
        generatedNew = true;
        return generator.GenerateChunk(pos.X, pos.Y, pos.Z);
    }

    private void SaveChunk(Chunk chunk)
    {
        storage.SaveChunk(chunk);
    }
    private void UnloadChunk(Vector3i pos)
    {
        this.chunks.Remove(pos, out var _);
        renderer.NotifyChunkDeleted(pos);
    }
    Task? updateAsyncTask;
    public void Update(Vector3i playerChunk)
    {
        //All of the updating happens asynchronously.
        if(updateAsyncTask is null || updateAsyncTask.IsCompleted)
        {
            updateAsyncTask = Task.Run(() =>
            {
                UpdateAsync(playerChunk);
            });
        }
        renderer.Update(this);
    }

    private DateTime LastStorageFlush;
    private void UpdateAsync(Vector3i playerChunk)
    {
        var horizontalLoadDistance = Program.Game.Settings.horizontalLoadDistance;
        var verticalLoadDistance = Program.Game.Settings.verticalLoadDistance;
        using var _ = Profiler.Push("UpdateAsync");
        float horizontalLoadDistanceSquared = horizontalLoadDistance*horizontalLoadDistance;
        //NOTE: this is the position of the chunk the player is in.
        // NOT the actual exact position of the player
        Vector3 playerPos = MathBits.GetChunkWorldPosUncentered(playerChunk);
        Profiler.PushRaw("ChunkUnload");
        List<Vector3i> chunksToUnload = new List<Vector3i>();
        foreach(var c in chunks)
        {
            var chunkWorldPos = MathBits.GetChunkWorldPos(c.Key);
            if(((playerPos - chunkWorldPos) * new Vector3(1, horizontalLoadDistance/verticalLoadDistance, 1)).LengthSquared > horizontalLoadDistanceSquared)
            {
                chunksToUnload.Add(c.Key);
            }
        }
        foreach(var c in chunksToUnload)
        {
            UnloadChunk(c);
        }
        Profiler.PopRaw("ChunkUnload");
        Profiler.PushRaw("ChunksFinishedLoading");
        //Profiler.PushRaw("PausePool");
        //pool.Pause();
        //Profiler.PopRaw("PausePool");
        lock(chunksFinishedLoading)
        {
            foreach(var chunk in chunksFinishedLoading)
            {
                chunksBeingLoaded.Remove(chunk.pos);
                if(!chunks.TryAdd(chunk.pos, chunk)){
                    System.Console.Error.WriteLine("Failed to add chunk " + chunk.pos);
                    continue;
                }
            }
            renderer.NotifyChunksAdded(chunksFinishedLoading);
            chunksFinishedLoading.Clear();
        }
        //pool.Unpause();
        Profiler.PopRaw("ChunksFinishedLoading");
        Profiler.PushRaw("ChunkLoadList");
        //It may be called a queue, but it's actually behaves more like a sorted bag.
        PriorityQueue<Vector3i, float> chunkLoadList = new PriorityQueue<Vector3i, float>();
        Vector3i chunkRange = MathBits.GetChunkPos(new Vector3(horizontalLoadDistance, verticalLoadDistance, horizontalLoadDistance));
        for(int cx=-chunkRange.X; cx<chunkRange.X; ++cx)
        {
            for(int cy=-chunkRange.Y; cy<chunkRange.Y; ++cy)
            {
                for(int cz=-chunkRange.Z; cz<chunkRange.Z; ++cz)
                {
                    var chunkPos = new Vector3i(cx, cy, cz) + playerChunk;
                    var chunkDistanceSquared =((playerPos - MathBits.GetChunkWorldPos(chunkPos)) * new Vector3(1, horizontalLoadDistance/verticalLoadDistance, 1)).LengthSquared;
                    if(
                    chunkDistanceSquared < horizontalLoadDistanceSquared &&
                    !chunksBeingLoaded.Contains(chunkPos) &&
                    !chunks.ContainsKey(chunkPos)
                    ){
                        chunkLoadList.Enqueue(chunkPos, chunkDistanceSquared);
                    }
                }
            }
        }
        Profiler.PopRaw("ChunkLoadList");
        Profiler.PushRaw("ChunkStartLoad");
        while(chunkLoadList.Count > 0)
        {
            var chunkPos = chunkLoadList.Dequeue();
            chunksBeingLoaded.Add(chunkPos);
            pool.SubmitTask(() => {
                var chunk = LoadChunk(chunkPos, out var isNew);
                chunk.Optimize();
                if(isNew)
                {
                    lock(modifiedChunks)modifiedChunks.Add(chunk);
                }
                lock(chunksFinishedLoading)chunksFinishedLoading.Add(chunk);
            }, "LoadChunk");
        }
        Profiler.PopRaw("ChunkStartLoad");
        Profiler.PushRaw("SaveModifiedChunks");
        lock(modifiedChunks)
        {
            foreach(var chunk in modifiedChunks)
            {
                pool.SubmitTask(() => {
                    SaveChunk(chunk);
                }, "SaveChunk");
            }
            modifiedChunks.Clear();
        }
        Profiler.PopRaw("SaveModifiedChunks");
        if(DateTime.Now - LastStorageFlush > Program.Game.Settings.chunkFlushPeriod)
        {
            Profiler.PushRaw("FlushStorage");
            LastStorageFlush = DateTime.Now;
            storage.Flush();
            Profiler.PopRaw("FlushStorage");
        }
    }

    public void Draw(Camera cam, Vector3i playerChunk)
    {
        renderer.DrawChunks(cam, playerChunk);
    }
    public IReadOnlyDictionary<Vector3i, Chunk> Chunks{get => chunks;}
    public int NumChunks {get => chunks.Count;}
    public int NumChunkSections {get => storage.NumberOfCachedSections;}
    public Chunk? GetChunk(Vector3i position)
    {
        if(chunks.TryGetValue(position, out var c)){
            return c;
        }
        return null;
    }
    public Chunk? GetChunk(int x, int y, int z)
    {
        if(chunks.TryGetValue(new Vector3i(x, y, z), out var c)){
            return c;
        }
        return null;
    }

    public Block? GetBlock(Vector3i pos)
    {
        //First, figure out which chunk the block is in
        Chunk? chunk = GetChunk(
            MathBits.DivideFloor(pos.X, Chunk.Size),
            MathBits.DivideFloor(pos.Y, Chunk.Size),
            MathBits.DivideFloor(pos.Z, Chunk.Size)
        );
        if(chunk is null)
        {
            return null;
        }
        //Then get the actual block itself
        return chunk.GetBlock(MathBits.Mod(pos.X, Chunk.Size), MathBits.Mod(pos.Y, Chunk.Size), MathBits.Mod(pos.Z, Chunk.Size));
    }

    public Block? GetBlock(Vector3i chunkPos, uint x, uint y, uint z)
    {
        //First, figure out which chunk the block is in
        Chunk? chunk = GetChunk(chunkPos);
        if(chunk is null)
        {
            return null;
        }
        //Then get the actual block itself
        return chunk.GetBlock(x, y, z);
    }

    public Block? GetBlock(Vector3i chunkPos, int x, int y, int z)
    {
        //First, figure out which chunk the block is in
        // TODO: find an integer-only version of this
        Vector3i chunkOffset = new Vector3i(
            (int)MathF.Floor(((float)x)/Chunk.Size),
            (int)MathF.Floor(((float)y)/Chunk.Size),
            (int)MathF.Floor(((float)z)/Chunk.Size)
        );
        Chunk? chunk = GetChunk(chunkPos + chunkOffset);
        if(chunk is null)
        {
            return null;
        }
        //Then get the actual block itself
        return chunk.GetBlock(MathBits.Mod(x, Chunk.Size), MathBits.Mod(y, Chunk.Size), MathBits.Mod(z, Chunk.Size));
    }

    public bool TrySetBlock(Block block, Vector3i blockPos)
    {
        //First, figure out which chunk the block is in
        Chunk? chunk = GetChunk(MathBits.GetBlockChunkPos(blockPos));
        if(chunk is null)
        {
            return false;
        }
        //Then set the actual block itself
        chunk.SetBlock(block, MathBits.Mod(blockPos.X, Chunk.Size), MathBits.Mod(blockPos.Y, Chunk.Size), MathBits.Mod(blockPos.Z, Chunk.Size));
        lock(modifiedChunks)modifiedChunks.Add(chunk);
        renderer.NotifyChunkModified(chunk.pos);
        return true;
    }

    public void Dispose()
    {
        renderer.Dispose();
        pool.Stop();
        storage.Flush();
    }
}