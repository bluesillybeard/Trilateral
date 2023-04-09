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

//An object that represents a chunk
class ChunkDrawObject{
    private List<(RenderModel model, IRenderShader shader)>? drawables;
    private List<ChunkBuildObject>? builds;
    public DateTime LastUpdate; //when the chunk was last updated
    public bool InProgress;
    public readonly Vector3i pos;

    public ChunkDrawObject(Vector3i pos)
    {
        InProgress = false;
        this.pos = pos;
    }

    public void Dispose()
    {
        if(drawables is not null)
        foreach(var d in drawables)
        {
            d.model.mesh.Dispose();
            //We don't dispose textures since they are persistent.
        }
    }
    public void Build(Vector3i pos, Chunk[] adjacent)
    {
        LastUpdate = DateTime.Now;
        Profiler.Push("ChunkBuild");
        Chunk chunk = adjacent[0];
        builds = new List<ChunkBuildObject>();
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
                    if(!buildObject.AddBlock(x, y, z, block, pos, adjacent))
                    {
                        //If adding the block failed (for whatever reason), cancel building this chunk.
                        return;
                    }
                }
            }
        }
        Profiler.Pop("ChunkBuild");
    }

    public ExecutorTask? SendToGPU()
    {
        if(builds is null) return null;
        return VRender.Render.SubmitToQueue(
            () => {
                var newdrawables = new List<(RenderModel model, IRenderShader shader)>();
                //We only have to wait for the very last task to finish
                foreach(ChunkBuildObject build in builds)
                {
                    var cpuMesh = build.mesh.ToMesh();
                    var mesh = VRender.Render.LoadMesh(cpuMesh);
                    newdrawables.Add((new RenderModel(mesh, build.texture), build.shader));
                }
                drawables = newdrawables;
            }, "UploadMesh" + pos
        );
    }

    public void Draw(Matrix4 cameraTransform)
    {
        //We need to make a translation for the position, since the mesh is only relative to the chunk's origin, not the actual origin.
        Matrix4 transform = Matrix4.CreateTranslation(pos.X * Chunk.Size * MathBits.XScale, pos.Y * Chunk.Size * 0.5f, pos.Z * Chunk.Size * 0.25f);
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