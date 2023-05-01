namespace Trilateral.World;

using Trilateral.Utility;

public interface IChunkGenerator
{

    public Chunk GenerateChunk(int x, int y, int z);
}