namespace Voxelesque.World.ChunkGenerators;

using World;
using Voxelesque.Utility;

public class BasicChunkGenerator : IChunkGenerator
{
    public Block fill;
    public Block empty;
    public FastNoiseLite noise;

    public BasicChunkGenerator(Block fill, Block empty, FastNoiseLite noise)
    {
        this.fill = fill;
        this.empty = empty;
        this.noise = noise;

    }
    public Chunk GenerateChunk(int x, int y, int z)
    {
        int csy = (int) (Chunk.Size * y);
        int csx = (int) (Chunk.Size * x);
        int csz = (int) (Chunk.Size * z * 0.5773502692f);
        Block[] blocks = new Block[Chunk.Size*Chunk.Size*Chunk.Size];
        for(uint xp = 0; xp < Chunk.Size; xp++){
            for(uint zp = 0; zp < Chunk.Size; zp++){
                float height = noise.GetNoise(csx+xp, csz+(zp*0.5773502692f));
                height = height*height*400;//squaring it makes it better by making lower terrain flatter, and higher terrain more varied and mountain-like
                for(uint yp = 0; yp < Chunk.Size; yp++){
                    uint index = xp + Chunk.Size*yp + Chunk.Size*Chunk.Size*zp;
                    if(csy+yp < height) {
                        blocks[index] = fill;
                    } else {
                        blocks[index] = empty;
                    }
                }
            }
        }
        Chunk c = new Chunk(blocks);
        return c;
    }
}