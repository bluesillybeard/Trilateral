namespace Trilateral.World;

using OpenTK.Mathematics;
using VRenderLib.Interface;
using System;
using vmodel;
using Utility;
using StbImageSharp;

//I honestly can't think of any other ways to optimize this code.
// I suppose it IS a pretty complex process. I just can't think of a good way to make it simpler.
// At the very least, it's bearably slow.
// TODO: a dev with lots of optimization experience, be it future me or others, make this code as fast as possible.
// Chunk building is a pretty big bottleneck in the world loading process.
readonly struct ChunkBuildObject{
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

    public bool AddBlock(uint bx, uint by, uint bz, IBlock block, Chunk[] adjacent)
    {
        var blockedFaces = GetBlockedFaces(bx, by, bz, adjacent);
        //skip surrounded blocks
        if((~blockedFaces & 0b11111) == 0){
            return true;
        }
        var blockMesh = block.Model.mesh;
        var totalAttribs = blockMesh.attributes.TotalAttributes();
        //TODO: try to convert the attributes if they don't match
        // Although, anybody making a block mesh should use the same attributes.
        if(!blockMesh.attributes.Equals(ChunkRenderer.chunkAttributes))
        {
            System.Console.Error.WriteLine("Block mesh attributes don't match required attributes");
            return false;
        }
        //Triangles have this really annoying property where their tesselation is annoyingly complex to calculate.
        // My old protytype used a hack to make it work, but this time i'm doing it "properly".
        var parity = ((bx+bz) & 1);
        //Every other block is rotated in order to meet the tesselation
        var angle = (MathF.PI/3)*parity;
        //And offset it by a certain amount, since tesselating triangles is driving me bloody insane
        //TODO: calculate this offset to greater accuruacy
        var XOffset = 0.144f*parity;
        Span<float> transformedVertex = stackalloc float[(int)totalAttribs];
        for(uint indexIndex = 0; indexIndex < blockMesh.indices.Length; indexIndex++)
        {
            //using var p = Profiler.Push("vertex");
            if (blockMesh.triangleToFaces is not null && (blockMesh.triangleToFaces[indexIndex / 3] & blockedFaces) != 0) {
                continue; // Skip this index if it should be removed
            }
            uint index = blockMesh.indices[indexIndex];
            var vertex = new  Span<float>(blockMesh.vertices, (int)(index*totalAttribs), (int)totalAttribs);
            vertex.CopyTo(transformedVertex);
            //SinCos made zero performance difference. It may be a hot path, but it's fast enough to be negligible.
            (var sina, var cosa) = MathF.SinCos(angle);
            transformedVertex[0] = vertex[0] *  cosa + vertex[2] * sina + bx * MathBits.XScale + XOffset;
            transformedVertex[1] = vertex[1]                                       + by * 0.5f;
            transformedVertex[2] = vertex[0] * -sina + vertex[2] * cosa + bz * 0.25f;
            mesh.AddVertex(transformedVertex);
            //p.Dispose();
        }
        return true;
    }

    private static byte GetBlockedFaces(uint bx, uint by, uint bz, Chunk[] adjacent)
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
            int num = (int)(i + 5*((bz + bx) & 1));
            int xm = 0;
            int ym = 0;
            int zm = 0;
            // I literally figured this out using trial and error lol
            switch(num)
            {
                //Not rotated
                case 0:
                    ym = 1; break;
                case 1:
                    ym = -1; break;
                case 2:
                    zm = -1; break;
                case 3:
                    zm = 1; break;
                case 4:
                    xm = -1; break;
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
                    zm = 1; break;
            }
            xm += (int)bx;
            ym += (int)by;
            zm += (int)bz;
            Vector3i chunkOffset = new(
                (int)MathF.Floor(((float)xm)/Chunk.Size),
                (int)MathF.Floor(((float)ym)/Chunk.Size),
                (int)MathF.Floor(((float)zm)/Chunk.Size)
            );
            int index = ChunkDrawObject.GetAdjacencyIndex(chunkOffset);
            var adjacentChunk = adjacent[index];
            var adjacentBlock = adjacentChunk.GetBlock(MathBits.Mod(xm, Chunk.Size),MathBits.Mod(ym, Chunk.Size),MathBits.Mod(zm, Chunk.Size));
            if (adjacentBlock is null)
            {
                continue;
            }
            byte adjacentOpaque = adjacentBlock.Model.opaqueFaces ?? 0;
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