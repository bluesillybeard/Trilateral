namespace Voxelesque.World;

using OpenTK.Mathematics;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System;
using vmodel;
using VRenderLib;
using VRenderLib.Utility;
using VRenderLib.Threading;
using Utility;

public sealed class ChunkRenderer
{
    public static readonly Attributes chunkAttributes = new Attributes(new EAttribute[]{EAttribute.position, EAttribute.textureCoords, EAttribute.normal});
    private Dictionary<Vector3i, ChunkDrawObject> chunkDrawObjects;
    private UnorderedLocalThreadPool chunkDrawPool;
    private ConcurrentDictionary<Vector3i, ChunkDrawObject> inProgressDrawObjects;
    private List<ChunkDrawObject> newDrawObjects;
    private List<Vector3i> chunksInWait; //Chunks that didn't have all their neighbors, so they are waiting for their neighbors.

    public ChunkRenderer()
    {
        chunkDrawObjects = new Dictionary<Vector3i, ChunkDrawObject>();
        inProgressDrawObjects = new ConcurrentDictionary<Vector3i, ChunkDrawObject>();
        chunkDrawPool = new UnorderedLocalThreadPool(Environment.ProcessorCount/2);
        newDrawObjects = new List<ChunkDrawObject>();
        chunksInWait = new List<Vector3i>();
    }

    public void DrawChunks(Camera camera, Vector3i playerChunk)
    {
        foreach(KeyValuePair<Vector3i, ChunkDrawObject> obj in chunkDrawObjects)
        {
            var pos = obj.Key - playerChunk;
            var drawObject = obj.Value;
            
            drawObject.Draw(camera.GetTransform());
        }
    }

    public void NotifyChunkDeleted(Vector3i pos)
    {
        if(inProgressDrawObjects.Remove(pos, out var inProgress))
        {
            if(!inProgress.InProgress)
            {
                //TODO: find a better way to do this.
                newDrawObjects.Remove(new ChunkDrawObject(pos));
            }
        }
        if(this.chunkDrawObjects.Remove(pos, out var drawObject)){
            drawObject.Dispose();
        }
    }

    public void NotifyChunkAdded(Vector3i pos, ChunkManager m)
    {
        //First, make sure we don't already have this chunk
        if(chunkDrawObjects.ContainsKey(pos) || inProgressDrawObjects.ContainsKey(pos))return;

        var chunks = GetAdjacentChunks(m, pos);
        //Chunks that aren't ready to be build go into the list
        if(chunks is null)
        {
            chunksInWait.Add(pos);
            return;
        }
        BuildChunk(pos, chunks);
    }

    private void BuildChunk(Vector3i pos, Chunk[] chunks)
    {
        var obj = new ChunkDrawObject(pos);
        obj.InProgress = true;
        //Would be great if there was a method that would add or replace.
        inProgressDrawObjects.AddOrUpdate(pos, 
        (p) => {
            return obj;
        },
        (p, old) => {
            return obj;
        });

        chunkDrawPool.SubmitTask(
            () => {
                obj.Build(pos, chunks);
                newDrawObjects.Add(obj);
            }, "BuildChunk"+pos
        );
    }

    public void Update(ChunkManager chunkManager)
    {
        Profiler.Push("ChunkRendererUpdate");
        //First, go through the chunks waiting to be built
        List<Vector3i> noLongerWaiting = new List<Vector3i>();
        foreach(var pos in chunksInWait)
        {
            var adj = GetAdjacentChunks(chunkManager, pos);
            if(adj is null)continue;
            BuildChunk(pos, adj);
            noLongerWaiting.Add(pos);
        }
        foreach(var pos in noLongerWaiting)
        {
            chunksInWait.Remove(pos);
        }
        //Then, go through all of the new chunks
        foreach(var newDraw in newDrawObjects)
        {
            inProgressDrawObjects.Remove(newDraw.pos, out var _);
            chunkDrawObjects.TryAdd(newDraw.pos, newDraw);
            newDraw.SendToGPU();
        }
        newDrawObjects.Clear();
        //For every chunk in the manager
        // foreach(var pair in chunkManager.Chunks)
        // {
        //     var pos = pair.Key;
        //     var chunk = pair.Value;
        //     //If the chunk has been built before but needs to be updated
        //     if(chunkDrawObjects.TryGetValue(pos, out var oldDrawObject))
        //     {
        //         if(oldDrawObject.LastUpdate < chunk.LastChange)
        //         {
        //             var adjacentChunks = GetAdjacentChunks(chunkManager, pos);
        //             if(adjacentChunks is null)continue;
        //             if(!oldDrawObject.InProgress)
        //             {
        //                 oldDrawObject.InProgress = true;
        //                 chunkDrawPool.SubmitTask(() => {oldDrawObject.Build(pos, adjacentChunks);}, "RebuildChunk");
        //             }
        //         }
        //     }
        //     else
        //     {
        //         var adjacentChunks = GetAdjacentChunks(chunkManager, pos);
        //         if(adjacentChunks is null)continue;
        //         var draw = new ChunkDrawObject();
        //         if(!chunkDrawObjects.TryAdd(pos, draw)){
        //             System.Console.WriteLine("Failed to add draw object for " + pos);
        //             continue;
        //         }
        //         draw.InProgress = true;
        //         chunkDrawPool.SubmitTask(() => {draw.Build(pos, adjacentChunks);}, "BuildChunk");
        //     }
        // }
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