namespace Trilateral.World.ChunkGenerators;

using World;

//Generates a singular block at 0,0,0

public class OneBlockChunkGenerator : IChunkGenerator
{
    public Block fill;

    public OneBlockChunkGenerator(Block fill)
    {
        this.fill = fill;
    }
    public Chunk GenerateChunk(int x, int y, int z)
    {
        Chunk c = new Chunk(new OpenTK.Mathematics.Vector3i(x, y, z), Program.Game.voidBlock);
        if(x ==0 && y == 0 && z == 0)
        {
            uint bx = 0;
            for(uint bz = 0; bz<Chunk.Size; bz++)
            {
                c.SetBlock(fill, bx, 0, bz);
                c.SetBlock(fill, bx, 1, bz);
            }
            
        }
        return c;
    }
}