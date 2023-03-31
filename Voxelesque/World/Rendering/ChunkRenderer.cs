namespace Voxelesque.World;

using OpenTK.Mathematics;
using System.Collections.Generic;
using vmodel;
using VRender;
using VRender.Utility;
using Utility;

public sealed class ChunkRenderer
{
    public static readonly Attributes chunkAttributes = new Attributes(new EAttribute[]{EAttribute.position, EAttribute.textureCoords, EAttribute.normal});

    private Dictionary<Vector3i, ChunkDrawObject> chunkDrawObjects;
    public ChunkRenderer()
    {
        chunkDrawObjects = new Dictionary<Vector3i, ChunkDrawObject>();
    }

    public void DrawChunks(Camera camera, Vector3i playerChunk)
    {
        foreach(KeyValuePair<Vector3i, ChunkDrawObject> obj in chunkDrawObjects)
        {
            var pos = obj.Key - playerChunk;
            var drawObject = obj.Value;
            //We need to make a translation for the position, since the mesh is only relative to the chunk's origin, not the actual origin.
            var transform = Matrix4.CreateTranslation(pos.X * Chunk.Size * MathBits.XScale, pos.Y * Chunk.Size * 0.5f, pos.Z * Chunk.Size * 0.25f);
            //Now we draw it
            var uniforms = new KeyValuePair<string, object>[]{
                new KeyValuePair<string, object>("model", transform),
                new KeyValuePair<string, object>("camera", camera.GetTransform())
            };
            foreach(var drawable in drawObject.Drawables)
            {
                VRenderLib.Render.Draw(
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
        //For every chunk in the manager
        lock(chunkManager.Chunks)
        {
            foreach(var pair in chunkManager.Chunks)
            {
                var pos = pair.Key;
                var chunk = pair.Value;
                //If the chunk has been built before but needs to be updated
                if(chunkDrawObjects.TryGetValue(pos, out var oldDrawObject))
                {
                    if(oldDrawObject.LastUpdate < chunk.LastChange && AllAdjacentChunksValid(pos, chunkManager))
                    {
                        oldDrawObject.BeginBuilding(pos, chunk, chunkManager);
                    }
                }
                else
                {
                    if(AllAdjacentChunksValid(pos, chunkManager))
                    {
                        //If the chunk has not been built before (It's a new chunk)
                        var draw = new ChunkDrawObject();
                        draw.BeginBuilding(pos, chunk, chunkManager);
                        chunkDrawObjects.Add(pos, draw);
                    }
                }
            }
        }
    }

    // private static readonly Vector3i[] adjacencyList = new Vector3i[]{
    //     new Vector3i( 0, 0, 1),
    //     new Vector3i( 0, 1, 0),
    //     new Vector3i( 1, 0, 0),
    //     new Vector3i( 0, 0,-1),
    //     new Vector3i( 0,-1, 0),
    //     new Vector3i(-1, 0, 0)
    // };
    /**
    <summary>
    Returns true if all 6 adjacent chunks are initialized
    </summary>
    */
    public static bool AllAdjacentChunksValid(Vector3i pos, ChunkManager m)
    {
        if(!m.Chunks.ContainsKey(new Vector3i(pos.X,   pos.Y,   pos.Z+1))) return false;
        if(!m.Chunks.ContainsKey(new Vector3i(pos.X,   pos.Y+1, pos.Z  ))) return false;
        if(!m.Chunks.ContainsKey(new Vector3i(pos.X+1, pos.Y,   pos.Z  ))) return false;
        if(!m.Chunks.ContainsKey(new Vector3i(pos.X,   pos.Y,   pos.Z-1))) return false;
        if(!m.Chunks.ContainsKey(new Vector3i(pos.X,   pos.Y-1, pos.Z  ))) return false;
        if(!m.Chunks.ContainsKey(new Vector3i(pos.X-1, pos.Y,   pos.Z  ))) return false;

        return true;
    }
}