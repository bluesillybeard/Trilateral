namespace Trilateral.World;

public interface IChunkGenerator
{

    public Chunk GenerateChunk(int x, int y, int z);
}