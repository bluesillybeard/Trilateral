namespace Trilateral.World;

using OpenTK.Mathematics;
using VRenderLib.Interface;
using System.Collections.Generic;
using System;
using VRenderLib.Threading;
using VRenderLib;
using Trilateral.Utility;

sealed class ChunkDrawObjectBuilding
{
    public ChunkDrawObjectBuilding(Vector3i pos)
    {
        this.pos = pos;
        builds = new List<ChunkBuildObject>();
        LastUpdate = DateTime.Now;
    }
    public readonly DateTime LastUpdate;
    public readonly Vector3i pos;
    public readonly List<ChunkBuildObject> builds;
    public bool InProgress;
    public bool Cancelled;

    public void Build(Chunk[] chunks)
    {
        InProgress = true;
        if(Cancelled)return;
        Profiler.PushRaw("ChunkBuild");
        Chunk chunk = chunks[0];
        for(uint x=0; x<Chunk.Size; x++){
            for(uint y=0; y<Chunk.Size; y++){
                for(uint z=0; z<Chunk.Size; z++){
                    IBlock block = chunk.GetBlock(x, y, z);
                    if(!block.Draw){
                        continue;
                    }
                    int buildObjectHash = ChunkBuildObject.HashCodeOf(block.Texture, block.Shader);
                    var index = builds.FindIndex((obj) => obj.GetHashCode() == buildObjectHash);
                    if(index == -1){
                        index = builds.Count;
                        builds.Add(new ChunkBuildObject(block.Texture, block.Shader, block.Model.texture));
                    }
                    var buildObject = builds[index];
                    if(!buildObject.AddBlock(x, y, z, block, chunks))
                    {
                        //If adding the block failed (for whatever reason), cancel building this chunk.
                        return;
                    }
                }
            }
        }
        Profiler.PopRaw("ChunkBuild");
        //There is a possibility it was cancelled during the process of building
        InProgress = false;
        if(Cancelled)return;
    }
}

sealed class ChunkDrawObjectUploading
{
    public ChunkDrawObjectUploading(Vector3i pos, DateTime time)
    {
        this.pos = pos;
        this.time = time;
        drawables = new List<(RenderModel model, IRenderShader shader)>();
    }
    public readonly DateTime time;
    public Vector3i pos;
    public bool InProgress;
    public bool Cancelled;
    public List<(RenderModel model, IRenderShader shader)> drawables;

    public ExecutorTask SendToGPU(ChunkDrawObjectBuilding built)
    {
        var builds = built.builds;
        InProgress = true;
        return VRender.Render.SubmitToQueueLowPriority(
            () => {
                if(Cancelled){
                    InProgress = false;
                    return;
                }
                Profiler.PushRaw("UploadChunk");
                foreach(ChunkBuildObject build in builds)
                {
                    var cpuMesh = build.mesh.ToMesh();
                    var mesh = VRender.Render.LoadMesh(cpuMesh);
                    drawables.Add((new RenderModel(mesh, build.texture), build.shader));
                }
                InProgress = false;
                if(Cancelled)
                {
                    foreach(var (model, shader) in drawables)
                    {
                        model.mesh.Dispose();
                    }
                }
                Profiler.PopRaw("UploadChunk");
            }, "UploadMesh" + pos
        );
    }
}

sealed class ChunkDrawObject
{
    public readonly DateTime LastUpdate; //when the chunk was last updated
    public readonly Vector3i pos;
    public readonly List<(RenderModel model, IRenderShader shader)> drawables;

    public ChunkDrawObject(ChunkDrawObjectUploading obj)
    {
        this.pos = obj.pos;
        this.LastUpdate = obj.time;
        this.drawables = obj.drawables;
    }

    public void Dispose()
    {
        foreach(var (model, _) in drawables)
        {
            model.mesh.Dispose();
        }
    }
    public void Draw(Matrix4 cameraTransform, Vector3i playerChunk, IDrawCommandQueue drawCommandQueue)
    {
        //Skip if there are no meshes to draw
        if(drawables.Count == 0)return;
        var offset = pos - playerChunk;
        //We need to make a translation for the position, since the mesh is only relative to the chunk's origin, not the actual origin.
        Matrix4 transform = Matrix4.CreateTranslation(MathBits.GetChunkWorldPosUncentered(offset));
        //Now we draw it
        var uniforms = new KeyValuePair<string, object>[]{
            new KeyValuePair<string, object>("model", transform),
            new KeyValuePair<string, object>("camera", cameraTransform)
        };
        if(drawables is null)return;
        foreach(var (model, shader) in drawables)
        {
            drawCommandQueue.Draw(
                model.texture, model.mesh,
                shader, uniforms, true
            );
        }
    }

    public static readonly Vector3i[] adjacencyList = new Vector3i[]{
        new Vector3i( 0, 0, 0),
        new Vector3i( 0, 0, 1),
        new Vector3i( 0, 1, 0),
        new Vector3i( 1, 0, 0),
        new Vector3i( 0, 0,-1),
        new Vector3i( 0,-1, 0),
        new Vector3i(-1, 0, 0)
    };
    //TODO: use a switch statement instead?
    private static readonly int[] reverseAdjacency = new int[]{
        -1,//-1,-1,-1
        -1,// 0,-1,-1
        -1,// 1,-1,-1
        -1,//-1, 0,-1
         4,// 0, 0,-1
        -1,// 1, 0,-1
        -1,//-1, 1,-1
        -1,// 0, 1,-1
        -1,// 1, 1,-1
        -1,//-1,-1, 0
         5,// 0,-1, 0
        -1,// 1,-1, 0
         6,//-1, 0, 0
         0,// 0, 0, 0
         3,// 1, 0, 0
        -1,//-1, 1, 0
         2,// 0, 1, 0
        -1,// 1, 1, 0
        -1,//-1,-1, 1
        -1,// 0,-1, 1
        -1,// 1,-1, 1
        -1,//-1, 0, 1
         1,// 0, 0, 1
        -1,// 1, 0, 1
        -1,//-1, 1, 1
        -1,// 0, 1, 1
        -1,// 1, 1, 1
    };
    //Make sure to update this if the adjacency list ever changes!
    public static int GetAdjacencyIndex(Vector3i adjacency)
    {
        int num = (adjacency.X+1) + 3*(adjacency.Y+1) + 9*(adjacency.Z+1);
        return reverseAdjacency[num];
    }

    public override bool Equals(object? obj)
    {
        if(obj is ChunkDrawObject o)
        {
            return o.pos.Equals(this.pos);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return pos.GetHashCode();
    }
}
