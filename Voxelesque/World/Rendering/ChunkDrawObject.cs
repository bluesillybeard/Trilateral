namespace Voxelesque.World;

using OpenTK.Mathematics;
using VRender.Interface;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.IO;
using vmodel;
using VRender;

//An object that represents a chunk
struct ChunkDrawObject{


    public List<(RenderModel model, IRenderShader shader)> Drawables;
    public DateTime LastUpdate; //when the chunk was last updated
    private Task? UpdateTask;
    private bool inProgress;
    public bool InProgress {get => inProgress;}

    public ChunkDrawObject()
    {
        inProgress = false;
        Drawables = new List<(RenderModel model, IRenderShader shader)>(0);
        LastUpdate = DateTime.Now;
        UpdateTask = null;
    }

    public void BeginBuilding(Vector3i pos, Chunk chunk, ChunkManager m)
    {
        //TODO: cancel current task and make a new one
        if(inProgress)return;
        inProgress = true;
        LastUpdate = DateTime.Now;
        var me = this;
        UpdateTask = Task.Run(
            () => {me.Build(pos, chunk, m);}
        );
    }
    //TODO: construct a list of the 6 adjacent chunks instead of looking at the global chunk manager
    private void Build(Vector3i pos, Chunk chunk, ChunkManager m)
    {
        if(!ChunkRenderer.AllAdjacentChunksValid(pos, m))
        {
            //If a chunk was unloaded while this chunk was waiting to be built, cancel building it.
            return;
        }
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
                    if(!buildObject.AddBlock(x, y, z, block, pos, chunk, m))
                    {
                        //If adding the block failed (for whatever reason), cancel building this chunk.
                        inProgress = false;
                        return;
                    }
                }
            }
        }

        foreach(ChunkBuildObject build in objects)
        {
            var shader = build.shader;
            var cpuMesh = build.mesh.ToMesh();
            var mesh = VRenderLib.Render.LoadMesh(cpuMesh);
            var texture = build.texture;
            Drawables.Add((new RenderModel(mesh, texture), shader));
        }
        inProgress = false;
    }

    private static void SaveFile(string path, VModel model)
    {
        try{
            //For the same of simplicity, I just make that path as a directory then save our files into it.
            if(!Directory.Exists(path))
                Directory.CreateDirectory(path);
            string name = new DirectoryInfo(path).Name;
            //save the files
            VModelUtils.SaveModel(model, path, "mesh.vmesh", "texture.png", "model.vmf");
        } catch(Exception e){
            System.Console.WriteLine("Could not save file: " + e.StackTrace + "\n " + e.Message);
        }
    }

    public void Dispose()
    {
        foreach(var d in Drawables)
        {
            d.model.mesh.Dispose();
            //We don't dispose textures since they are persistent.
        }
    }
}