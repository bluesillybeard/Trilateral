namespace Voxelesque.World;

using OpenTK.Mathematics;
using VRenderLib.Interface;
using System.Collections.Generic;
using System;
using VRenderLib.Threading;
using System.IO;
using vmodel;
using VRenderLib;
using Voxelesque.Utility;

class ChunkDrawObjectBuilding
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
        Profiler.Push("ChunkBuild");
        Chunk chunk = chunks[0];        
        for(uint x=0; x<Chunk.Size; x++){
            for(uint y=0; y<Chunk.Size; y++){
                for(uint z=0; z<Chunk.Size; z++){
                    Block? blockOrNone = chunk.GetBlock(x, y, z);
                    if(blockOrNone is null){
                        continue;
                    }
                    Block block = blockOrNone;
                    if(!block.draw)continue;
                    int buildObjectHash = ChunkBuildObject.HashCodeOf(block.texture, block.shader);
                    var index = builds.FindIndex((obj) => {return obj.GetHashCode() == buildObjectHash;});
                    if(index == -1){
                        index = builds.Count;
                        builds.Add(new ChunkBuildObject(block.texture, block.shader, block.model.texture));
                    }
                    var buildObject = builds[index];
                    if(!buildObject.AddBlock(x, y, z, block, pos, chunks))
                    {
                        //If adding the block failed (for whatever reason), cancel building this chunk.
                        return;
                    }
                }
            }
        }
        Profiler.Pop("ChunkBuild");
        //There is a possibility it was cancelled during the process of building
        InProgress = false;
        if(Cancelled)return;
    }
}

class ChunkDrawObjectUploading
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

    public void SendToGPU(ChunkDrawObjectBuilding built)
    {
        var builds = built.builds;
        if(Cancelled)return;
        InProgress = true;
        VRender.Render.SubmitToQueue(
            () => {
                if(Cancelled)return;
                foreach(ChunkBuildObject build in builds)
                {
                    var cpuMesh = build.mesh.ToMesh();
                    var mesh = VRender.Render.LoadMesh(cpuMesh);
                    drawables.Add((new RenderModel(mesh, build.texture), build.shader));
                }
                InProgress = false;
                if(Cancelled)
                {
                    foreach(var d in drawables)
                    {
                        d.model.mesh.Dispose();
                    }
                }
            }, "UploadMesh" + pos
        );

    }
}

class ChunkDrawObject
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
        foreach(var drawable in drawables)
        {
            drawable.model.mesh.Dispose();
        }
    }
    public void Draw(Matrix4 cameraTransform, Vector3i playerChunk)
    {
        var offset = pos - playerChunk;
        //We need to make a translation for the position, since the mesh is only relative to the chunk's origin, not the actual origin.
        Matrix4 transform = Matrix4.CreateTranslation(offset.X * Chunk.Size * MathBits.XScale, offset.Y * Chunk.Size * 0.5f, offset.Z * Chunk.Size * 0.25f);
        //Now we draw it
        var uniforms = new KeyValuePair<string, object>[]{
            new KeyValuePair<string, object>("model", transform),
            new KeyValuePair<string, object>("camera", cameraTransform)
        };
        if(drawables is null)return;
        foreach(var drawable in drawables)
        {
            
            VRender.Render.Draw(
                drawable.model.texture, drawable.model.mesh,
                drawable.shader, uniforms, true
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
    //TODO: use a switch statement instead
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
