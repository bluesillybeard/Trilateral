namespace Voxelesque.World;

using OpenTK.Mathematics;
using VRender.Interface;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.IO;
using vmodel;
using VRender;
using Voxelesque.Utility;

//An object that represents a chunk
struct ChunkDrawObject{
    public List<(RenderModel model, IRenderShader shader)> Drawables;
    public DateTime LastUpdate; //when the chunk was last updated
    private Task? UpdateTask;
    public bool InProgress {get => UpdateTask is not null && !UpdateTask.IsCompleted;}

    public ChunkDrawObject()
    {
        Drawables = new List<(RenderModel model, IRenderShader shader)>(0);
        LastUpdate = DateTime.Now;
        UpdateTask = null;
    }

    public void BeginBuilding(Vector3i pos, Chunk[] adjacent)
    {
        Profiler.Push("ChunkBeginBuilding");
        //TODO: cancel current task and make a new one
        if(InProgress)return;
        LastUpdate = DateTime.Now;
        var me = this;
        UpdateTask = Task.Run(
            () => {
                try{
                    me.Build(pos, adjacent);
                } catch (Exception e)
                {
                    System.Console.Error.WriteLine("Error while building chunk: " + e.Message);
                }
            }
        );
        Profiler.Pop("ChunkBeginBuilding");
    }

    public void Dispose()
    {
        foreach(var d in Drawables)
        {
            d.model.mesh.Dispose();
            //We don't dispose textures since they are persistent.
        }
    }
    private void Build(Vector3i pos, Chunk[] adjacent)
    {
        Profiler.Push("ChunkBuild");
        Profiler.Push("ChunkBuildBlocks");
        DateTime startTime = DateTime.Now;
        Chunk chunk = adjacent[0];
        var objects = new List<ChunkBuildObject>();
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
                    var index = objects.FindIndex((obj) => {return obj.GetHashCode() == buildObjectHash;});
                    if(index == -1){
                        index = objects.Count;
                        objects.Add(new ChunkBuildObject(block.texture, block.shader, block.model.texture));
                    }
                    var buildObject = objects[index];
                    if(!buildObject.AddBlock(x, y, z, block, pos, adjacent))
                    {
                        //If adding the block failed (for whatever reason), cancel building this chunk.
                        return;
                    }
                }
            }
        }
        Profiler.Pop("ChunkBuildBlocks");
        Profiler.Push("ChunkBuildMesh");
        //We only have to wait for the very last task to finish
        
        foreach(ChunkBuildObject build in objects)
        {
            var cpuMesh = build.mesh.ToMesh();
            var mesh = VRenderLib.Render.LoadMesh(cpuMesh);
            Drawables.Add((new RenderModel(mesh, build.texture), build.shader));
        }
        Profiler.Pop("ChunkBuildMesh");
        Profiler.Pop("ChunkBuild");
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
}