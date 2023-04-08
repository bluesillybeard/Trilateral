namespace Voxelesque.World;

using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using System;

using OpenTK.Mathematics;

using Utility;
using VRenderLib.Utility;
public sealed class ChunkManager
{
    private ConcurrentDictionary<Vector3i, Chunk> chunks;
    private ChunkRenderer renderer;
    private IChunkGenerator generator;
    public ChunkManager(IChunkGenerator generator)
    {
        //ThreadPool.SetMaxThreads(8, 8);
        this.generator = generator;
        chunks = new ConcurrentDictionary<Vector3i, Chunk>();//new Dictionary<Vector3i, Chunk>();
        renderer = new ChunkRenderer();
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
    private Task? UpdateTask;
    public void Update(Vector3 playerPosition, float distance)
    {
        if(UpdateTask is null || UpdateTask.IsCompleted)
        {
            Profiler.Push("ChunkUpdateBegin");
            UpdateTask = Task.Run(() => {
                try{
                    UpdateSync(playerPosition, distance);
                    renderer.Update(this);
                } catch (Exception e)
                {
                    System.Console.Error.WriteLine("Exception: " + e.Message + "\nstack trace:" + e.StackTrace);
                }
            });
            Profiler.Pop("ChunkUpdateBegin");
        }
    }

    private void UpdateSync(Vector3 playerPosition, float distance)
    {
        Profiler.Push("ChunkUpdate");
        Vector3i chunkRange = MathBits.GetChunkPos(new Vector3(distance, distance, distance));
        Vector3i playerChunk = MathBits.GetChunkPos(playerPosition);
        float distanceSquared = distance * distance;
        UnloadDistantChunks(playerPosition, distanceSquared);
        //First, generate a list of chunks that need to be loaded.
        // TODO: If there are multiple players, this code will absolute screw up massively.
        int totalChunks = 2*2*2*chunkRange.X*chunkRange.Y*chunkRange.Z - chunks.Count;
        if(totalChunks == 0)return;
        Profiler.Push("GenerateLoadList");
        List<Vector3i> chunksToLoad = new List<Vector3i>(totalChunks);
        for(int cx=-chunkRange.X; cx<chunkRange.X; cx++)
        {
            for (int cy = -chunkRange.Y; cy < chunkRange.Y; cy++)
            {
                for (int cz = -chunkRange.Z; cz < chunkRange.Z; cz++)
                {
                    Vector3i chunkPos = playerChunk + new Vector3i(cx, cy, cz);
                    if(!chunks.ContainsKey(chunkPos))
                    {
                        chunksToLoad.Add(chunkPos);
                    }
                }
            }
        }
        Profiler.Pop("GenerateLoadList");
        Profiler.Push("LoadChunks");
        ParallelOptions options = new ParallelOptions();
        //Eating up all the threads in the world is a bad idea, it causes the game thread and render thread to have less time allocated to them,
        // Decreasing framerate for no real reason.
        options.MaxDegreeOfParallelism = Environment.ProcessorCount/2;
        Parallel.For(0, chunksToLoad.Count, options, (index) => {
            Vector3i chunkToLoad = chunksToLoad[index];
            UpdateChunk(chunkToLoad, distanceSquared, playerPosition);
        });
        Profiler.Pop("LoadChunks");
        Profiler.Pop("ChunkUpdate");
    }

    private void UnloadDistantChunks(Vector3 playerPosition, float distanceSquared)
    {
        Profiler.Push("UnloadChunks");
        List<Vector3i> chunksToRemove = new List<Vector3i>();
        lock(chunks){
            foreach(var chunk in chunks)
            {
                var pos = chunk.Key;
                var data = chunk.Value;
                Vector3 chunkWorldPos = MathBits.GetChunkWorldPos(pos);
                if(Vector3.DistanceSquared(chunkWorldPos, playerPosition) > distanceSquared)
                {
                    chunksToRemove.Add(pos);
                }
            }
            foreach(Vector3i c in chunksToRemove)
            {
                UnloadChunk(c);
            }
        }
        Profiler.Pop("UnloadChunks");
    }

    private void UpdateChunk(Vector3i chunkPos, float distanceSquared, Vector3 playerPosition)
    {
        Profiler.Push("UpdateChunkExistence");
        Vector3 chunkWorldPos = MathBits.GetChunkWorldPos(chunkPos);
        if(Vector3.DistanceSquared(chunkWorldPos, playerPosition) < distanceSquared)
        {
            var chunk = LoadChunk(chunkPos);
            chunks.AddOrUpdate(chunkPos, (a) => {
                return chunk;
            }, (pos, existing) => {
                if(existing.LastChange <= chunk.LastChange) return chunk;
                return existing;
            }); 
        }
        Profiler.Pop("UpdateChunkExistence");
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
}