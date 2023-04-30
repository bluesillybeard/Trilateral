namespace Voxelesque.World;

using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using System;

using OpenTK.Mathematics;

using Utility;
using VRenderLib.Utility;
using VRenderLib.Threading;
public sealed class ChunkManager
{
    private readonly ConcurrentDictionary<Vector3i, Chunk> chunks;
    public readonly ChunkRenderer renderer;
    private readonly IChunkGenerator generator;
    private readonly LocalThreadPool pool;
    //TODO: Figure out the best data structure for this.
    private readonly HashSet<Vector3i> chunksBeingLoaded; 
    private readonly ConcurrentBag<Chunk> chunksFinishedLoading;
    public ChunkManager(IChunkGenerator generator)
    {
        this.generator = generator;
        chunks = new ConcurrentDictionary<Vector3i, Chunk>();//new Dictionary<Vector3i, Chunk>();
        renderer = new ChunkRenderer();
        pool = new LocalThreadPool(int.Max(1, Environment.ProcessorCount/2));
        chunksBeingLoaded = new HashSet<Vector3i>();
        chunksFinishedLoading = new ConcurrentBag<Chunk>();
    }
    private Chunk LoadChunk(Vector3i pos)
    {
        Profiler.Push("LoadChunk");
        var chunk = generator.GenerateChunk(pos.X, pos.Y, pos.Z);
        Profiler.Pop("LoadChunk");
        return chunk;
    }
    private void UnloadChunk(Vector3i pos)
    {
        this.chunks.Remove(pos, out var _);
        renderer.NotifyChunkDeleted(pos);
    }
    public void Update(Vector3i playerChunk, float loadDistance)
    {
        float loadDistanceSquared = loadDistance*loadDistance;
        //NOTE: this is the position of the chunk the player is in.
        // NOT the actual exact position of the player
        Vector3 playerPos = MathBits.GetChunkWorldPos(playerChunk);
        Profiler.Push("ChunkUnload");
        List<Vector3i> chunksToUnload = new List<Vector3i>();
        foreach(var c in chunks)
        {
            var chunkWorldPos = MathBits.GetChunkWorldPos(c.Key);
            if(Vector3.DistanceSquared(playerPos, chunkWorldPos) > loadDistanceSquared)
            {
                chunksToUnload.Add(c.Key);
            }
        }
        foreach(var c in chunksToUnload)
        {
            UnloadChunk(c);
        }
        Profiler.Pop("ChunkUnload");
        Profiler.Push("ChunksFinishedLoading");
        pool.Pause();
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
        pool.Unpause();
        Profiler.Pop("ChunksFinishedLoading");
        Profiler.Push("ChunkLoadList");
        //It may be called a queue, but it's actually behaves more like a sorted bag.
        PriorityQueue<Vector3i, float> chunkLoadList = new PriorityQueue<Vector3i, float>();
        Vector3i chunkRange = MathBits.GetChunkPos(new Vector3(loadDistance, loadDistance, loadDistance));
        for(int cx=-chunkRange.X; cx<chunkRange.X; ++cx)
        {
            for(int cy=-chunkRange.Y; cy<chunkRange.Y; ++cy)
            {
                for(int cz=-chunkRange.Z; cz<chunkRange.Z; ++cz)
                {
                    var chunkPos = new Vector3i(cx, cy, cz) + playerChunk;
                    var chunkDistance = Vector3.DistanceSquared(playerPos, MathBits.GetChunkWorldPos(chunkPos));
                    if(
                    chunkDistance < loadDistanceSquared &&
                    !chunksBeingLoaded.Contains(chunkPos) &&
                    !chunks.ContainsKey(chunkPos)
                    ){
                        chunkLoadList.Enqueue(chunkPos, chunkDistance);
                    }
                }
            }
        }
        Profiler.Pop("ChunkLoadList");
        Profiler.Push("ChunkStartLoad");
        while(chunkLoadList.Count > 0)
        {
            var chunkPos = chunkLoadList.Dequeue();
            chunksBeingLoaded.Add(chunkPos);
            pool.SubmitTask(() => {
                var chunk = LoadChunk(chunkPos);
                chunk.Optimize();
                chunksFinishedLoading.Add(chunk);
            }, "LoadChunk");
        }
        Profiler.Pop("ChunkStartLoad");
        renderer.Update(this);
    }

    public void Draw(Camera cam, Vector3i playerChunk)
    {
        renderer.DrawChunks(cam, playerChunk);
    }
    public IReadOnlyDictionary<Vector3i, Chunk> Chunks{get => chunks;}
    public int NumChunks {get => chunks.Count;}
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

    public bool TrySetBlock(Block block, Vector3i pos)
    {
        //First, figure out which chunk the block is in
        Chunk? chunk = GetChunk(pos/(int)Chunk.Size);
        if(chunk is null)
        {
            return false;
        }
        //Then set the actual block itself
        chunk.SetBlock(block, MathBits.Mod(pos.X, Chunk.Size), MathBits.Mod(pos.Y, Chunk.Size), MathBits.Mod(pos.Z, Chunk.Size));
        return true;
    }

    public void Dispose()
    {
        renderer.Dispose();
        pool.Stop();
    }
}