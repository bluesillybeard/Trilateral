using nbtsharp;

namespace Trilateral.World.ChunkGenerators;

public interface IChunkGenerator
{

    public Chunk GenerateChunk(int x, int y, int z);
    public NBTFolder GetSettingsNBT(string folderName);
    public string GetId();
}