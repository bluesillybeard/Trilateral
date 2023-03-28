namespace Voxelesque.World;

using OpenTK.Mathematics;
using VRender.Interface;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using vmodel;
using VRender;
using StbImageSharp;

//An object that represents a chunk
struct ChunkDrawObject{


    public List<(RenderModel model, IRenderShader shader)> drawables;
    public DateTime LastUpdate; //when the chunk was last updated

    //TODO: account for adjacent chunks
    public ChunkDrawObject(Vector3i pos, Chunk chunk, ChunkManager chunkManager) : this()
    {
        drawables = new List<(RenderModel, IRenderShader)>();

        var objects = new List<ChunkBuildObject>();
        for(uint x=0; x<Chunk.Size; x++){
            for(uint y=0; y<Chunk.Size; y++){
                for(uint z=0; z<Chunk.Size; z++){
                    Block? blockOrNone = chunk.GetBlock(x, y, z);
                    if(blockOrNone is null){
                        continue;
                    }
                    Block block = blockOrNone.Value;
                    int buildObjectHash = ChunkBuildObject.HashCodeOf(block.model.texture, block.shader);
                    var index = objects.FindIndex((obj) => {return obj.GetHashCode() == buildObjectHash;});
                    if(index == -1){
                        index = objects.Count;
                        objects.Add(new ChunkBuildObject(block.model.texture, block.shader));
                    }
                    var buildObject = objects[index];
                    var blockedFaces = GetBlockedFaces(x, y, z, pos, chunk, chunkManager);
                    buildObject.AddBlock(x, y, z, block, blockedFaces);
                }
            }
        }
        LastUpdate = DateTime.Now;
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
            var adjacentBlock = chunkManager.GetBlock(new Vector3i(xm + chunkPos.X, ym + chunkPos.Y, zm + chunkPos.Z));
            byte adjacentOpaque = 0;
            if(adjacentBlock is not null)
            {
                adjacentOpaque = adjacentBlock.Value.model.opaqueFaces ?? 0;
            }
            blockedFaces |= (byte)(adjacentOpaque & (1>>i));
        }
        return blockedFaces;
    }

    public void Dispose()
    {
        foreach(var d in drawables)
        {
            d.model.mesh.Dispose();
            //We don't dispose textures since they are persistent.
        }
    }
}


struct ChunkBuildObject{

    
    public static int HashCodeOf(ImageResult texture, IRenderShader shader)
    {
        return 2*texture.GetHashCode() + 3*shader.GetHashCode();
    }
    public ChunkBuildObject(ImageResult texture, IRenderShader shader)
    {
        this.texture = texture;
        this.shader = shader;
        this.hash = HashCodeOf(texture, shader);
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
        float XMirror = ((x + z) & 1) - 0.5f; //-1 if X should be mirrored, 1 if it shouldn't.

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
                vertex[0] * XMirror + x * 0.5f,
                vertex[1] * 0.5f + y * 0.5f,
                vertex[2] * 0.5f + z * 0.288675134595f,
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
    public readonly ImageResult texture;
    public readonly IRenderShader shader;
    // The hash is computationally expensive and is based on objects that won't change,
    // So we calculate it once at the beginning and store it
    private readonly int hash;

    public override int GetHashCode()
    {
        return hash;
    }

    public void BeginAddToDrawObject(ChunkDrawObject obj)
    {
        var build = this;
        //We do a little "fire and forget" here.
        Task.Run(
            () => {
                build.AddToDrawObject(obj);
            }
        );
    }

    private void AddToDrawObject(ChunkDrawObject obj)
    {
        var renderModel = VRenderLib.Render.LoadModel(new VModel(this.mesh.ToMesh(), this.texture, null));
        obj.drawables.Add((renderModel, this.shader));
    }
}
public sealed class ChunkRenderer
{
    public static readonly Attributes chunkAttributes = new Attributes(new EAttribute[]{EAttribute.position, EAttribute.textureCoords, EAttribute.normal});

    private Dictionary<Vector3i, ChunkDrawObject> chunkDrawObjects;

    private List<KeyValuePair<Vector3i, ChunkDrawObject>> modifiedDrawObjects;
    public ChunkRenderer()
    {
        chunkDrawObjects = new Dictionary<Vector3i, ChunkDrawObject>();
        modifiedDrawObjects = new List<KeyValuePair<Vector3i, ChunkDrawObject>>();
    }

    public void DrawChunks()
    {
        foreach(var obj in modifiedDrawObjects)
        {
            var pos = obj.Key;
            var draw = obj.Value;
            if(chunkDrawObjects.TryGetValue(pos, out var old))
            {
                old.Dispose();
            }
            chunkDrawObjects[pos] = draw;
        }
        modifiedDrawObjects.Clear();
        System.Console.WriteLine(chunkDrawObjects.Count);
        foreach(KeyValuePair<Vector3i, ChunkDrawObject> obj in chunkDrawObjects)
        {
            var pos = obj.Key;
            var drawObject = obj.Value;
            //We need to make a translation for the position, since the mesh is only relative to the chunk's origin, not the actual origin.
            var transform = Matrix4.CreateTranslation(pos.X * Chunk.Size * 0.28867513459481288225f, pos.Y * Chunk.Size * 0.5f, pos.Z * Chunk.Size * 0.5f);
            //Now we draw it
            var uniforms = new KeyValuePair<string, object>[]{new KeyValuePair<string, object>("model", transform)};
            foreach(var drawable in drawObject.drawables)
            {
                VRenderLib.Render.Draw(
                    drawable.model.texture, drawable.model.mesh,
                    drawable.shader, uniforms, true
                );
            }
        }
    }

    public void NotifyChunkUpdated(Vector3i pos, Chunk chunk, ChunkManager chunkManager)
    {
        BuildChunkAsync(pos, chunk, chunkManager);
    }
    private void BuildChunkAsync(Vector3i pos, Chunk chunk, ChunkManager chunkManager)
    {
        //TODO: special executor just for building chunks
        Task.Run(
            () => {BuildChunk(pos, chunk, chunkManager);}
        );
    }

    private void BuildChunk(Vector3i pos, Chunk chunk, ChunkManager chunkManager)
    {
        //I normally wouldn't put a try-catch here,
        // But since this is part of a "fire and forget" method, we need to make sure any exceptions are caught and logged properly.
        try{
            modifiedDrawObjects.Add(new KeyValuePair<Vector3i, ChunkDrawObject>(pos, new ChunkDrawObject(pos, chunk, chunkManager)));
        } catch (Exception e)
        {
            System.Console.Error.WriteLine("Exception while building chunk:" + e.Message + "\n Stacktrace:" + e.StackTrace);
        }
    }
}