namespace Voxelesque.World;

using Voxelesque.Utility;

public interface IChunkGenerator
{

    public Chunk GenerateChunk(int x, int y, int z);
}