namespace Voxelesque.World;

using OpenTK.Mathematics;
using VRender.Interface;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.IO;
using vmodel;
using VRender;
using VRender.Utility;
using Utility;
using StbImageSharp;

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
    private void Build(Vector3i pos, Chunk chunk, ChunkManager m)
    {
        var objects = new List<ChunkBuildObject>();
        for(uint x=0; x<Chunk.Size; x++){
            for(uint y=0; y<Chunk.Size; y++){
                for(uint z=0; z<Chunk.Size; z++){
                    Block? blockOrNone = chunk.GetBlock(x, y, z);
                    if(blockOrNone is null){
                        continue;
                    }
                    Block block = blockOrNone.Value;
                    if(!block.draw)continue;
                    int buildObjectHash = ChunkBuildObject.HashCodeOf(block.texture, block.shader);
                    var index = objects.FindIndex((obj) => {return obj.GetHashCode() == buildObjectHash;});
                    if(index == -1){
                        index = objects.Count;
                        objects.Add(new ChunkBuildObject(block.texture, block.shader, block.model.texture));
                    }
                    var buildObject = objects[index];
                    var blockedFaces = GetBlockedFaces(x, y, z, pos, chunk, m);
                    buildObject.AddBlock(x, y, z, block, blockedFaces);
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
    private byte GetBlockedFaces(uint x, uint y, uint z, Vector3i chunkPos, Chunk chunk, ChunkManager chunkManager)
    {
        /*
        bit 1 :top (+y)
        bit 2 :bottom(-y)
        bit 4 :side 1 (+x)
        bit 8 :side 2 (+x rotated 60 degrees towards +z)
        bit 16:side 3 (+x rotated 60 degrees towards -z)
        */
        byte blockedFaces = 0;
        bool flip = ((z + x) & 1) == 1;
        //If flip is true, we flip along the X axis.
        // So basically the X coordinates are multiplied by -1.
        
        //My old protytype flipped the Z axis, but I already made my new block model assuming the X axis flips, so oops I guess.
        for(int i=0; i<5; i++)
        {
            int xm = 0;
            int ym = 0;
            int zm = 0;
            switch(i)
            {
                case 0:
                    ym = 1; break;
                case 1:
                    ym = -1; break;
                case 2:
                    xm = flip ? -1 : 1; break;
                case 3:
                    zm = 1; break;
                case 4:
                    zm = -1; break;
            }
            xm += (int)x;
            ym += (int)y;
            zm += (int)z;
            //TODO: chunkManager method that makes this more better
            var adjacentBlock = chunkManager.GetBlock(chunkPos, xm, ym, zm);
            byte adjacentOpaque = 0;
            if(adjacentBlock is not null)
            {
                adjacentOpaque = adjacentBlock.Value.model.opaqueFaces ?? 0;
            }
            blockedFaces |= (byte)(adjacentOpaque & (1<<i));
        }
        return blockedFaces;
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


struct ChunkBuildObject{

    
    public static int HashCodeOf(IRenderTexture texture, IRenderShader shader)
    {
        int a = texture.GetHashCode();
        int b = shader.GetHashCode();
        a ^= b >> 5;
        a ^= b << 17;
        a ^= b >> 23;
        a ^= b << 27;
        return a;
    }
    public ChunkBuildObject(IRenderTexture texture, IRenderShader shader, ImageResult cpuTexture)
    {
        this.texture = texture;
        this.shader = shader;
        this.hash = HashCodeOf(texture, shader);
        this.CPUTexture = cpuTexture;
        mesh = new MeshBuilder(ChunkRenderer.chunkAttributes);
    }

    public void AddBlock(uint x, uint y, uint z, Block block, byte blockedFaces)
    {
        //skip surrounded blocks
        if((~blockedFaces & 0b11111) == 0){
            return;
        }
        var blockMesh = block.model.mesh;
        var totalAttribs = blockMesh.attributes.TotalAttributes();
        //TODO: try to convert the attributes if they don't match
        if(!blockMesh.attributes.Equals(ChunkRenderer.chunkAttributes))
        {
            System.Console.Error.WriteLine("Block mesh attributes don't match required attributes");
            return;
        }
        float XMirror = ((x + z) & 1) - 0.5f; //-0.5 if X should be mirrored, 0.5 if it shouldn't.

        for(uint indexIndex = 0; indexIndex < blockMesh.indices.Length; indexIndex++)
        {
            if (blockMesh.triangleToFaces is not null && (blockMesh.triangleToFaces[indexIndex / 3] & blockedFaces) != 0) {
                continue; // Skip this index if it should be removed
            }
            uint index = blockMesh.indices[indexIndex];
            //Not really sure why, but I can't use a span. My guess is that the AsSpan method isn't implemented for floats.
            //Span<float> vertex = mesh.vertices.AsSpan<float>(index*totalAttribs, totalAttribs);
            float[] vertex = blockMesh.vertices[(int)(index*totalAttribs) .. (int)(index*totalAttribs+totalAttribs)];
            mesh.AddVertex(
                //x, y, z
                vertex[0] * 2*XMirror + x * 0.5f,
                vertex[1] + y * 0.5f,
                vertex[2] + z * MathBits.ZScale,
                //We leave normals and texture coordinates as-is
                vertex[3],
                vertex[4],
                vertex[5],
                vertex[6],
                vertex[7]
            );
        }
    }

    public readonly MeshBuilder mesh;
    public readonly IRenderTexture texture;
    public readonly IRenderShader shader;
    public readonly ImageResult CPUTexture;
    // The hash is computationally expensive and is based on objects that won't change,
    // So we calculate it once at the beginning and store it
    private readonly int hash;

    public override int GetHashCode()
    {
        return hash;
    }
}
public sealed class ChunkRenderer
{
    public static readonly Attributes chunkAttributes = new Attributes(new EAttribute[]{EAttribute.position, EAttribute.textureCoords, EAttribute.normal});

    private Dictionary<Vector3i, ChunkDrawObject> chunkDrawObjects;
    public ChunkRenderer()
    {
        chunkDrawObjects = new Dictionary<Vector3i, ChunkDrawObject>();
    }

    public void DrawChunks(Camera camera)
    {
        foreach(KeyValuePair<Vector3i, ChunkDrawObject> obj in chunkDrawObjects)
        {
            var pos = obj.Key;
            var drawObject = obj.Value;
            //We need to make a translation for the position, since the mesh is only relative to the chunk's origin, not the actual origin.
            var transform = Matrix4.CreateTranslation(pos.X * Chunk.Size * 0.5f, pos.Y * Chunk.Size * 0.5f, pos.Z * Chunk.Size * MathBits.ZScale);
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
    /**
    <summary>
    Returns true if all 6 adjacent chunks are initialized
    </summary>
    */
    private bool AllAdjacentChunksValid(Vector3i pos, ChunkManager m)
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