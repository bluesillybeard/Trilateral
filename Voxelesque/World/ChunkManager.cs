namespace Voxelesque.World;

using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System;

using OpenTK.Mathematics;

using Utility;
using VRender.Utility;
public sealed class ChunkManager
{
    private ConcurrentDictionary<Vector3i, Chunk> chunks;
    private ChunkRenderer renderer;
    private IChunkGenerator generator;
    public ChunkManager(IChunkGenerator generator)
    {
        this.generator = generator;
        chunks = new ConcurrentDictionary<Vector3i, Chunk>();//new Dictionary<Vector3i, Chunk>();
        renderer = new ChunkRenderer();
    }
    private Chunk LoadChunk(Vector3i pos)
    {
        var chunk = generator.GenerateChunk(pos.X, pos.Y, pos.Z);
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
            UpdateTask = Task.Run(() => {
                try{
                    UpdateSync(playerPosition, distance);
                    renderer.Update(this);
                } catch (Exception e)
                {
                    System.Console.Error.WriteLine("Exception: " + e.Message + "\nstack trace:" + e.StackTrace);
                }
            });
        }
    }

    private void UpdateSync(Vector3 playerPosition, float distance)
    {
        Vector3i chunkRange = MathBits.GetChunkPos(new Vector3(distance, distance, distance));
        Vector3i playerChunk = MathBits.GetChunkPos(playerPosition);
        float distanceSquared = distance * distance;
        UnloadDistantChunks(playerPosition, distanceSquared);
        List<KeyValuePair<Vector3i, Chunk>> newChunks = new List<KeyValuePair<Vector3i, Chunk>>();
        Parallel.For(-chunkRange.X, chunkRange.X, (chunkX) =>
        {
            for (int chunkY = -chunkRange.Y; chunkY < chunkRange.Y; chunkY++)
            {
                for (int chunkZ = -chunkRange.Z; chunkZ < chunkRange.Z; chunkZ++)
                {
                    Vector3i chunkPos = playerChunk + new Vector3i(chunkX, chunkY, chunkZ);
                    UpdateChunk(chunkPos, distanceSquared, playerPosition);
                }
            }
        });
    }

    private void UnloadDistantChunks(Vector3 playerPosition, float distanceSquared)
    {
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
    }

    private void UpdateChunk(Vector3i chunkPos, float distanceSquared, Vector3 playerPosition)
    {
        if(chunks.ContainsKey(chunkPos))
        {
            return;
        }
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
    }
    public void Draw(Camera cam, Vector3i playerChunk)
    {
        renderer.DrawChunks(cam, playerChunk);
    }
    public IReadOnlyDictionary<Vector3i, Chunk> Chunks{get => chunks;}
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