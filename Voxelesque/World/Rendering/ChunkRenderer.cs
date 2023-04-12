namespace Voxelesque.World;

using OpenTK.Mathematics;
using System.Collections.Generic;
using System.Collections.Concurrent;
using vmodel;
using VRenderLib;
using VRenderLib.Utility;
using Utility;

public sealed class ChunkRenderer
{
    public static readonly Attributes chunkAttributes = new Attributes(new EAttribute[]{EAttribute.position, EAttribute.textureCoords, EAttribute.normal});

    private ConcurrentDictionary<Vector3i, ChunkDrawObject> chunkDrawObjects;
    public ChunkRenderer()
    {
        chunkDrawObjects = new ConcurrentDictionary<Vector3i, ChunkDrawObject>();
    }

    public void DrawChunks(Camera camera, Vector3i playerChunk)
    {
        foreach(KeyValuePair<Vector3i, ChunkDrawObject> obj in chunkDrawObjects)
        {
            var pos = obj.Key - playerChunk;
            var drawObject = obj.Value;
            if(drawObject.InProgress)
            {
                continue; //Skip ones that aren't finished
            }
            //We need to make a translation for the position, since the mesh is only relative to the chunk's origin, not the actual origin.
            var transform = Matrix4.CreateTranslation(pos.X * Chunk.Size * MathBits.XScale, pos.Y * Chunk.Size * 0.5f, pos.Z * Chunk.Size * 0.25f);
            //Now we draw it
            var uniforms = new KeyValuePair<string, object>[]{
                new KeyValuePair<string, object>("model", transform),
                new KeyValuePair<string, object>("camera", camera.GetTransform())
            };
            foreach(var drawable in drawObject.Drawables)
            {
                VRender.Render.Draw(
                    drawable.model.texture, drawable.model.mesh,
                    drawable.shader, uniforms, true
                );
            }
        }
    }

    public void NotifyChunkDeleted(Vector3i pos)
    {
        if(this.chunkDrawObjects.Remove(pos, out var drawObject)){
            drawObject.Dispose();
        }
    }

    public void Update(ChunkManager chunkManager)
    {
        Profiler.Push("ChunkRendererUpdate");
        //For every chunk in the manager
        foreach(var pair in chunkManager.Chunks)
        {
            var pos = pair.Key;
            var chunk = pair.Value;
            //If the chunk has been built before but needs to be updated
            if(chunkDrawObjects.TryGetValue(pos, out var oldDrawObject))
            {
                if(oldDrawObject.LastUpdate < chunk.LastChange)
                {
                    var adjacentChunks = GetAdjacentChunks(chunkManager, pos);
                    if(adjacentChunks is null)continue;
                    oldDrawObject.BeginBuilding(pos, adjacentChunks);
                }
            }
            else
            {
                var adjacentChunks = GetAdjacentChunks(chunkManager, pos);
                if(adjacentChunks is null)continue;
                var draw = new ChunkDrawObject();
                if(!chunkDrawObjects.TryAdd(pos, draw)){
                    System.Console.WriteLine("Failed to add draw object for " + pos);
                    continue;
                }
                draw.BeginBuilding(pos, adjacentChunks);
            }
        }
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
}