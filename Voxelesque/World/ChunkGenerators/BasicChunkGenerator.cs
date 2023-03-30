namespace Voxelesque.World.ChunkGenerators;

using World;
using Voxelesque.Utility;

public class BasicChunkGenerator : IChunkGenerator
{
    public Block fill;
    public FastNoiseLite noise;

    public BasicChunkGenerator(Block fill, FastNoiseLite noise)
    {
        this.fill = fill;
        this.noise = noise;

    }
    public Chunk GenerateChunk(int cx, int cy, int cz)
    {
        int csy = (int) (Chunk.Size * cy);
        int csx = (int) (Chunk.Size * cx);
        int csz = (int) (Chunk.Size * cz * 0.5773502692f);
        Block?[] blocks = new Block[Chunk.Size*Chunk.Size*Chunk.Size];
        for(uint xp = 0; xp < Chunk.Size; xp++){
            for(uint zp = 0; zp < Chunk.Size; zp++){
                float height = noise.GetNoise(csx+xp, csz+(zp*0.5773502692f));
                height = height*height*400;
                //squaring it makes it better by making lower terrain flatter, and higher terrain more varied and mountain-like
                for(uint yp = 0; yp < Chunk.Size; yp++){
                    uint index = xp + Chunk.Size*yp + Chunk.Size*Chunk.Size*zp;
                    if(csy+yp < height) {
                        blocks[index] = fill;
                    }
                }
            }
        }
        Chunk c = new Chunk(blocks);
        return c;
    }
}