namespace Voxelesque.World;

using OpenTK.Mathematics;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using vmodel;
using VRenderLib;
using VRenderLib.Utility;
using VRenderLib.Threading;
using Utility;

public sealed class ChunkRenderer
{
    public static readonly Attributes chunkAttributes = new Attributes(new EAttribute[]{EAttribute.position, EAttribute.textureCoords, EAttribute.normal});
    private UnorderedLocalThreadPool chunkDrawPool;
    //TODO: Use different types, instead of it all being draw objects.
    private Dictionary<Vector3i, ChunkDrawObject> chunkDrawObjects; //Chunks that are ready to be drawn.
    private Dictionary<Vector3i, ChunkDrawObjectUploading> chunksBeingUploaded; //Chunks that are in the provess of being uploaded to the GPU
    private Dictionary<Vector3i, ChunkDrawObjectBuilding> chunksBeingBuilt; //chunks that are in the process of being built
    private List<Vector3i> chunksInWait; //Chunks that are waiting to be built
    private ConcurrentBag<Vector3i> newChunks;
    private ConcurrentBag<Vector3i> chunksToRemove; //chunks that are waiting to be removed

    public ChunkRenderer()
    {
        chunkDrawObjects = new Dictionary<Vector3i, ChunkDrawObject>();
        chunksBeingUploaded = new Dictionary<Vector3i, ChunkDrawObjectUploading>();
        chunksBeingBuilt = new Dictionary<Vector3i, ChunkDrawObjectBuilding>();
        chunksInWait = new List<Vector3i>();
        newChunks = new ConcurrentBag<Vector3i>();
        chunksToRemove = new ConcurrentBag<Vector3i>();
        chunkDrawPool = new UnorderedLocalThreadPool();
    }

    public void DrawChunks(Camera camera, Vector3i playerChunk)
    {
        foreach(KeyValuePair<Vector3i, ChunkDrawObject> obj in chunkDrawObjects)
        {
            obj.Value.Draw(camera.GetTransform(), playerChunk);
        }
    }

    public void NotifyChunkDeleted(Vector3i pos)
    {
        chunksToRemove.Add(pos);
    }

    public void NotifyChunksAdded(IEnumerable<Chunk> chunks)
    {
        foreach(Chunk c in chunks)
        {
            //If the chunk is empty, just skip it.
            if(c.IsEmpty())return;
            newChunks.Add(c.pos);
        }
        
    }

    private void BuildChunk(Vector3i pos, Chunk[] chunks)
    {
        var obj = new ChunkDrawObjectBuilding(pos);
        chunksBeingBuilt.Add(pos, obj);
        obj.InProgress = true;
        chunkDrawPool.SubmitTask(() => {
            obj.Build(chunks);
        }, "BuildChunk"+pos);
    }

    public void Update(ChunkManager chunkManager)
    {
        Profiler.Push("ChunkRendererUpdate");
        Profiler.Push("ChunksToRemove");
        //Go through the chunks waiting to be removed
        foreach(var pos in chunksToRemove)
        {
            //Not sure why it's complaining about nullability here.
            // just ignore it for now.
            #nullable disable
            chunksInWait.Remove(pos);
            #nullable restore
            if(chunksBeingBuilt.Remove(pos, out var building))
            {
                building.Cancelled = true;
            }
            else if(chunksBeingUploaded.Remove(pos, out var uploading))
            {
                uploading.Cancelled = true;
            }
            else if(chunkDrawObjects.Remove(pos, out var draw))
            {
                draw.Dispose();
            }
        }
        chunksToRemove.Clear();
        Profiler.Pop("ChunksToRemove");
        Profiler.Push("ChunksInWait");
        //go through the chunks waiting to be built
        //If this line isn't here, it takes AGES to wait for this thread's turn to use ChunksInWait.
        List<Vector3i> noLongerWaiting = new List<Vector3i>();
        chunksInWait.AddRange(newChunks);
        newChunks.Clear();
        foreach(var pos in chunksInWait)
        {
            var adj = GetAdjacentChunks(chunkManager, pos);
            if(adj is null)
            {
                continue;
            }
            BuildChunk(pos, adj);
            noLongerWaiting.Add(pos);
        }
        Profiler.Pop("ChunksInWait");
        Profiler.Push("ChunksNoLongerWaiting");
        foreach(var pos in noLongerWaiting)
        {
            //Not sure why it's complaining about nullability here.
            // just ignore it for now.
            #nullable disable
            chunksInWait.Remove(pos);
            #nullable restore
        }
        Profiler.Pop("ChunksNoLongerWaiting");
        Profiler.Push("ChunksBeingBuilt");
        //Chunks that are being built or just finished building
        List<ChunkDrawObjectBuilding> chunksFinishedBuilding = new List<ChunkDrawObjectBuilding>();
        foreach(var chunk in chunksBeingBuilt)
        {
            if(!chunk.Value.InProgress)
            {
                //If it finished building
                chunksFinishedBuilding.Add(chunk.Value);
            }
        }
        foreach(var chunk in chunksFinishedBuilding)
        {
            chunksBeingBuilt.Remove(chunk.pos);
            var uploading = new ChunkDrawObjectUploading(chunk.pos, chunk.LastUpdate);
            chunksBeingUploaded.Add(chunk.pos, uploading);
            uploading.SendToGPU(chunk);
        }
        chunksFinishedBuilding.Clear();
        Profiler.Pop("ChunksBeingBuilt");
        Profiler.Push("ChunksBeingUploaded");
        List<ChunkDrawObjectUploading> chunksFinishedUploading = new List<ChunkDrawObjectUploading>();
        //Go through the chunks that are being uploaded or are done uploading
        foreach(var chunk in chunksBeingUploaded)
        {
            if(!chunk.Value.InProgress)
            {
                chunksFinishedUploading.Add(chunk.Value);
            }
        }
        foreach(var chunk in chunksFinishedUploading)
        {
            chunksBeingUploaded.Remove(chunk.pos);
            chunkDrawObjects.Add(chunk.pos, new ChunkDrawObject(chunk));
        }
        Profiler.Pop("ChunksBeingUploaded");
        Profiler.Pop("ChunkRendererUpdate");
    }

    private Chunk[]? GetAdjacentChunks(ChunkManager m, Vector3i pos)
    {
        Profiler.Push("GetAdjacentChunks");
        //If the chunk has not been built before (It's a new chunk)
        Chunk[] adjacentChunks = new Chunk[ChunkDrawObject.adjacencyList.Length];
        for(uint i=0; i<adjacentChunks.Length; i++)
        {
            var c = m.GetChunk(pos + ChunkDrawObject.adjacencyList[i]);
            if(c is null){
                Profiler.Pop("GetAdjacentChunks");
                return null; //We don't want to build chunks that don't have all adjacent ones available.
            }
            adjacentChunks[i] = c;
        }
        Profiler.Pop("GetAdjacentChunks");
        return adjacentChunks;
    }

    public void Dispose()
    {
        chunkDrawPool.Stop();
    }
}