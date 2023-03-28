namespace Voxelesque.World;

using System.Collections.Generic;
using OpenTK.Mathematics;

using Utility;
public sealed class ChunkManager
{
    private Dictionary<Vector3i, Chunk> chunks;
    private ChunkRenderer renderer;
    private IChunkGenerator generator;
    public ChunkManager(IChunkGenerator generator)
    {
        this.generator = generator;
        chunks = new Dictionary<Vector3i, Chunk>();
        renderer = new ChunkRenderer();
    }
    private Chunk LoadChunk(Vector3i pos)
    {
        var chunk = generator.GenerateChunk(pos.X, pos.Y, pos.Z);
        this.chunks[pos] = chunk;
        return chunk;
    }
    private void UnloadChunk(Vector3i pos)
    {
        this.chunks[pos] = generator.GenerateChunk(pos.X, pos.Y, pos.Z);
    }
    public void Update(Vector3 playerPosition, float distance)
    {
        Vector3i chunkRange = MathBits.GetChunkPos(new Vector3(distance, distance, distance));
        Vector3i playerChunk = MathBits.GetChunkPos(playerPosition);
        float distanceSquared = distance * distance;
        for (int x = -chunkRange.X; x < chunkRange.X; x++) {
            for (int y = -chunkRange.Y; y < chunkRange.Y; y++) {
                for (int z = -chunkRange.Z; z < chunkRange.Z; z++) {
                    Vector3i chunkPos = playerChunk + new Vector3i(x, y, z);
                    if(chunks.ContainsKey(chunkPos))continue;
                    Vector3 chunkWorldPos = MathBits.GetChunkWorldPos(chunkPos);
                    if(Vector3.DistanceSquared(chunkWorldPos, playerPosition) < distanceSquared)
                    {
                        var chunk = LoadChunk(chunkPos);
                        renderer.NotifyChunkUpdated(chunkPos, chunk, this);
                    }
                    
                    //if (RenderUtils.getChunkWorldPos(chunkPos).distanceSquared(GlobalBits.playerPosition) < renderDistanceSquared && !scheduledChunks.contains(chunkPos)) {
                    //    scheduledChunks.add(chunkPos);
                    //    //executor.submit(() -> {
                    //    //    loadChunk(chunkPos.x, chunkPos.y, chunkPos.z);
                        //    scheduledChunks.remove(chunkPos);
                        //});
                    //}
                }
            }
        }
    }

    public void Draw()
    {
        renderer.DrawChunks();
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
        Chunk? chunk = GetChunk(pos/(int)Chunk.Size);
        if(chunk is null)
        {
            return null;
        }
        //Then get the actual block itself
        return chunk.Value.GetBlock(MathBits.Mod(pos.X, Chunk.Size), MathBits.Mod(pos.Y, Chunk.Size), MathBits.Mod(pos.Z, Chunk.Size));
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
        chunk.Value.SetBlock(block, MathBits.Mod(pos.X, Chunk.Size), MathBits.Mod(pos.Y, Chunk.Size), MathBits.Mod(pos.Z, Chunk.Size));
        return true;
    }
}