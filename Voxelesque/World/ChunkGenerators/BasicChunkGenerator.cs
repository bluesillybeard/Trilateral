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
        int csz = (int) (Chunk.Size * z * 0.5773502692);
        int csy = (int)Chunk.Size * y;
        int csx = (int)Chunk.Size * x;
        Chunk c = new Chunk();
        for(uint xp = 0; xp < Chunk.Size; xp++){
            for(uint zp = 0; zp < Chunk.Size; zp++){
                double height = noise.GetNoise(csx+(xp * 0.5773502692f), csz+zp);
                height = height*height*400;//squaring it makes it better by making lower terrain flatter, and higher terrain more varied and mountain-like
                for(uint yp = 0; yp < Chunk.Size; yp++){
                    if(csy+yp < height) {
                        c.SetBlock(fill, xp, yp, zp);
                    } else {
                        c.SetBlock(empty, xp, yp, zp);
                    }
                }
            }
        }
        return c;
    }
}