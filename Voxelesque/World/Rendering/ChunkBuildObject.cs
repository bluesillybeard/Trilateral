namespace Voxelesque.World;

using OpenTK.Mathematics;
using VRender.Interface;
using System;
using vmodel;
using Utility;
using StbImageSharp;

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

    public bool AddBlock(uint bx, uint by, uint bz, Block block, Vector3i chunkPos, Chunk chunk, ChunkManager m)
    {
        var blockedFaces = GetBlockedFaces(bx, by, bz, chunkPos, chunk, m);
        if(blockedFaces == 255)return false;
        //skip surrounded blocks
        if((~blockedFaces & 0b11111) == 0){
            return true;
        }
        var blockMesh = block.model.mesh;
        var totalAttribs = blockMesh.attributes.TotalAttributes();
        //TODO: try to convert the attributes if they don't match
        if(!blockMesh.attributes.Equals(ChunkRenderer.chunkAttributes))
        {
            System.Console.Error.WriteLine("Block mesh attributes don't match required attributes");
            return false;
        }

        //Triangles have this really annoying property where their tesselation is annoyingly complex to calculate.
        // My old protytype used a hack to make it work, but this time i'm doing it "properly".
        var parity = ((bx+bz) & 1) == 1;
        var angle = 0f;
        var XOffset = 0f;
        if(parity)
        {
            //Rotate it by 60 degrees
            angle += MathF.PI/3;
            //And offset it by a certain amount, since tesselating triangles is driving me bloody insane
            //TODO: calculate this offset to greater accuruacy
            XOffset = 0.144f;
        }
        for(uint indexIndex = 0; indexIndex < blockMesh.indices.Length; indexIndex++)
        {
            //if (blockMesh.triangleToFaces is not null && (blockMesh.triangleToFaces[indexIndex / 3] & blockedFaces) != 0) {
            //    continue; // Skip this index if it should be removed
            //}
            uint index = blockMesh.indices[indexIndex];
            //Not really sure why, but I can't use a span. My guess is that the AsSpan method isn't implemented for floats.
            //Span<float> vertex = mesh.vertices.AsSpan<float>(index*totalAttribs, totalAttribs);
            float[] vertex = blockMesh.vertices[(int)(index*totalAttribs) .. (int)(index*totalAttribs+totalAttribs)];

            var sina = MathF.Sin(angle);
            var cosa = MathF.Cos(angle);
            Vector3 pos = new Vector3(vertex[0], vertex[1], vertex[2]);
            pos = new Vector3(
                pos.X *  cosa + pos.Z * sina + bx * MathBits.XScale + XOffset,
                pos.Y                        + by * 0.5f,
                pos.X * -sina + pos.Z * cosa + bz * 0.25f
            );
            mesh.AddVertex(
                //x, y, z
                pos.X,
                pos.Y,
                pos.Z,
                //We leave normals and texture coordinates as-is
                vertex[3],
                vertex[4],
                vertex[5],
                vertex[6],
                vertex[7]
            );
        }

        return true;
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
        for(int i=0; i<5; i++)
        {
            //Rotate every other block in a grid pattern by 60 degrees.
            int num = (int)(i + 5*((z + x) & 1));
            int xm = 0;
            int ym = 0;
            int zm = 0;
            switch(i)
            {
                //Not rotated
                case 0:
                    ym = 1; break;
                case 1:
                    ym = -1; break;
                case 2:
                    xm = 1; break;
                case 3:
                    zm = 1; break;
                case 4:
                    zm = -1; break;
                //rotated by 60 degrees
                case 5:
                    ym = 1; break;
                case 6:
                    ym = -1; break;
                case 7:
                    zm = -1; break;
                case 8:
                    xm = 1; break;
                case 9:
                    xm = -1; break;
            }
            xm += (int)x;
            ym += (int)y;
            zm += (int)z;
            var adjacentBlock = chunkManager.GetBlock(chunkPos, xm, ym, zm);
            byte adjacentOpaque = 0;
            if(adjacentBlock is null)
            {
                continue;
            }
            adjacentOpaque = adjacentBlock.model.opaqueFaces ?? 0;
            blockedFaces |= (byte)(adjacentOpaque & (1<<i));
        }
        return blockedFaces;
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