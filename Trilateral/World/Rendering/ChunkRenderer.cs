namespace Trilateral.World;

using OpenTK.Mathematics;
using System.Collections.Generic;
using System;
using vmodel;
using VRenderLib.Utility;
using VRenderLib.Threading;
using Utility;
using System.Threading.Tasks;
using VRenderLib.Interface;

public sealed class ChunkRenderer
{
    public static readonly Attributes chunkAttributes = new Attributes(new EAttribute[]{EAttribute.position, EAttribute.textureCoords, EAttribute.normal});
    private Dictionary<Vector3i, ChunkDrawObject> chunkDrawObjects; //Chunks that are ready to be drawn.
    public int DrawableChunks {get => chunkDrawObjects.Count;}
    private Dictionary<Vector3i, ChunkDrawObjectUploading> chunksUploading; //Chunks that are in the provess of being uploaded to the GPU
    public int UploadingChunks {get => chunksUploading.Count;}
    private Dictionary<Vector3i, ChunkDrawObjectBuilding> chunksBuilding; //chunks that are in the process of being built
    public int BuildingChunks {get => chunksBuilding.Count;}
    private List<Vector3i> chunksInWait; //Chunks that are waiting to be built
    public int WaitingChunks {get => chunksInWait.Count;}
    private List<Vector3i> newOrModifiedChunks;
    private List<Vector3i> chunksToRemove; //chunks that are waiting to be removed
    private List<Vector3i> otherChunksToRemove; //this swaps with chunksToRemove so that other threads can add chunks to remove without waiting for the other to be iterated.
    
    //this is required for two reasons:
    // 1: makes looking up of a chunk is somewhere faster
    // 2: Sometimes chunks are culled (chunks that have no renderable mesh), so they aren't in the renderer but they are still accounted for.
    private HashSet<Vector3i> chunksInRenderer; //Set of chunks that have been added but not removed.

    public ChunkRenderer(float renderThreadsMultiplier)
    {
        chunkDrawObjects = new Dictionary<Vector3i, ChunkDrawObject>();
        chunksUploading = new Dictionary<Vector3i, ChunkDrawObjectUploading>();
        chunksBuilding = new Dictionary<Vector3i, ChunkDrawObjectBuilding>();
        chunksInWait = new List<Vector3i>();
        newOrModifiedChunks = new List<Vector3i>();
        chunksToRemove = new List<Vector3i>();
        otherChunksToRemove = new List<Vector3i>();
        chunksInRenderer = new HashSet<Vector3i>();
    }

    public void DrawChunks(Camera camera, Vector3i playerChunk, IDrawCommandQueue drawCommandQueue)
    {
        foreach(KeyValuePair<Vector3i, ChunkDrawObject> obj in chunkDrawObjects)
        {
            obj.Value.Draw(camera.GetTransform(), playerChunk, drawCommandQueue);
        }
    }

    public void NotifyChunkDeleted(Vector3i pos)
    {
        lock(chunksToRemove)chunksToRemove.Add(pos);
    }

    public void NotifyChunkModified(Vector3i pos)
    {
        lock(newOrModifiedChunks)newOrModifiedChunks.Add(pos);
        lock(chunksInRenderer)chunksInRenderer.Add(pos);
    }

    public void NotifyChunksAdded(IEnumerable<Chunk> chunks)
    {
        Profiler.PushRaw("NotifyChunksAdded");
        foreach(Chunk c in chunks)
        {
            //TODO: cull chunks that will just end up being nothing
            if(chunksInRenderer.Contains(c.pos))
            {
                continue; //skip existing chunks
            }
            NotifyChunkModified(c.pos);
        }
        Profiler.PopRaw("NotifyChunksAdded");
    }

    private void BuildChunk(Vector3i pos, Chunk[] chunks)
    {
        var obj = new ChunkDrawObjectBuilding(pos);
        chunksBuilding.TryAdd(pos, obj);
        obj.InProgress = true;
        Task.Run(() => {
            obj.Build(chunks);
        });
    }
    public void Update(ChunkManager chunkManager)
    {
        Profiler.PushRaw("ChunkRendererUpdate");
        Profiler.PushRaw("ChunksToRemove");
        //Go through the chunks waiting to be removed
        // Swap between two lists so that the lock statement is as short as possible
        var t = otherChunksToRemove;
        otherChunksToRemove = chunksToRemove;
        lock(chunksToRemove)
        {
            chunksToRemove = t;
        }
        foreach(var pos in otherChunksToRemove)
        {
            lock(chunksInWait)chunksInWait.Remove(pos);
            ChunkDrawObject? draw = null;
            if(draw is not null)draw.Dispose();
            bool removedFromDraw = chunkDrawObjects.Remove(pos, out draw);
            bool removedFromBuilding = false;
            if(!removedFromDraw)
            {
                Profiler.PushRaw("lockBuilding");
                lock(chunksBuilding)
                {
                    Profiler.PopRaw("lockBuilding");
                    if(chunksBuilding.Remove(pos, out var building))
                    {
                        building.Cancelled = true;
                        removedFromBuilding = true;
                    }
                }
            }
            if(!removedFromBuilding)
            {
                Profiler.PushRaw("lockUploading");
                lock(chunksUploading)
                {
                    Profiler.PopRaw("lockUploading");
                    if(chunksUploading.Remove(pos, out var uploading))
                    {
                        uploading.Cancelled = true;
                    }
                }
            }
            lock(chunksInRenderer)chunksInRenderer.Remove(pos);
        }
        otherChunksToRemove.Clear();
        Profiler.PopRaw("ChunksToRemove");
        Profiler.PushRaw("ChunksInWait");
        lock(newOrModifiedChunks)
        {
            lock(chunksInWait)chunksInWait.AddRange(newOrModifiedChunks);
            newOrModifiedChunks.Clear();
        }
        //TODO: perhaps using a linked list might be faster
        lock(chunksInWait)
        {
            for(int i=chunksInWait.Count-1; i>=0; --i)
            {
                var pos = chunksInWait[i];
                var adj = GetAdjacentChunks(chunkManager, pos);
                if(adj is null)
                {
                    continue;
                }
                BuildChunk(pos, adj);
                chunksInWait.RemoveAt(i--);
            }
        }
        Profiler.PopRaw("ChunksInWait");
        Profiler.PushRaw("ChunksBeingBuilt");
        //Chunks that are being built or just finished building
        List<ChunkDrawObjectBuilding> chunksFinishedBuilding = new List<ChunkDrawObjectBuilding>();
        lock(chunksBuilding)
        {
            foreach(var chunk in chunksBuilding)
            {
                if(chunksFinishedBuilding.Count > Program.Game.Settings.maxChunkUpdatesPerFrame)
                {
                    break;
                }
                if(!chunk.Value.InProgress)
                {
                    //If it finished building
                    chunksFinishedBuilding.Add(chunk.Value);
                }
            }
        }
        foreach(var chunk in chunksFinishedBuilding)
        {
            lock(chunksBuilding)chunksBuilding.Remove(chunk.pos);
            var uploading = new ChunkDrawObjectUploading(chunk.pos, chunk.LastUpdate);
            if(!chunksUploading.TryAdd(chunk.pos, uploading))
            {
                System.Console.Error.WriteLine("ERROR: Failed to add ChunkDrawObjectUploading " + chunk.pos);
            }
            uploading.SendToGPU(chunk);
        }
        Profiler.PopRaw("ChunksBeingBuilt");
        Profiler.PushRaw("ChunksBeingUploaded");
        List<ChunkDrawObjectUploading> chunksFinishedUploading = new List<ChunkDrawObjectUploading>();
        //Go through the chunks that are being uploaded or are done uploading
        //TODO: possibly split lock into two sections
        lock(chunksUploading)
        {
            foreach(var chunk in chunksUploading)
            {
                if(!chunk.Value.InProgress)
                {
                    chunksFinishedUploading.Add(chunk.Value);
                }
            }
        }
        foreach(var chunk in chunksFinishedUploading)
        {
            lock(chunksUploading)chunksUploading.Remove(chunk.pos);
            var draw = new ChunkDrawObject(chunk);
            if(!chunkDrawObjects.TryAdd(chunk.pos, draw))
            {
                chunkDrawObjects[chunk.pos].Dispose();
                chunkDrawObjects[chunk.pos] = draw;
            }
        }
        Profiler.PopRaw("ChunksBeingUploaded");
        Profiler.PopRaw("ChunkRendererUpdate");
    }

    private Chunk[]? GetAdjacentChunks(ChunkManager m, Vector3i pos)
    {
        //If the chunk has not been built before (It's a new chunk)
        Chunk[] adjacentChunks = new Chunk[ChunkDrawObject.adjacencyList.Length];
        for(uint i=0; i<adjacentChunks.Length; i++)
        {
            var c = m.GetChunk(pos + ChunkDrawObject.adjacencyList[i]);
            if(c is null){
                return null; //We don't want to build chunks that don't have all adjacent ones available.
            }
            adjacentChunks[i] = c;
        }
        return adjacentChunks;
    }

    public void Dispose()
    {
    }
}