//Why on earth would I make an entire struct just to store a position in the world?
// You'll see.

using OpenTK.Mathematics;
using Trilateral.World;

public struct WorldPos
{

    public WorldPos(Vector3i chunk, Vector3 offset)
    {
        this.chunk = chunk;
        this.offset = offset;
    }
    public Vector3i chunk;
    public Vector3 offset;

    public override bool Equals(object? obj)
    {
        if(obj is WorldPos pos)
        {
            return pos.chunk == chunk && pos.offset == offset;
        }
        return false;
    }
    
    public override int GetHashCode()
    {
        return chunk.GetHashCode() * offset.GetHashCode();
    }

    public override string ToString()
    {
        return chunk.ToString() + "|" + offset.ToString();
    }

    public static bool operator== (WorldPos a, WorldPos b)
    {
        return a.chunk == b.chunk && a.offset == b.offset;
    }
    public static bool operator!= (WorldPos a, WorldPos b)
    {
        return a.chunk != b.chunk || a.offset != b.offset;
    }

    public static WorldPos operator+ (WorldPos a, WorldPos b)
    {
        WorldPos output = new WorldPos();
        output.chunk = a.chunk + b.chunk;
        output.offset = a.offset + b.offset;
        RestoreNormality(ref output);
        return output;
    }

    public static WorldPos operator- (WorldPos a, WorldPos b)
    {
        WorldPos output = new WorldPos();
        output.chunk = a.chunk - b.chunk;
        output.offset = a.offset - b.offset;
        RestoreNormality(ref output);
        return output;
    }

    //TODO: dot and cross products, since WorldPos is basically just a fancy way to store a vector
    public static void RestoreNormality(ref WorldPos pos)
    {
        var chunkDelta = (Vector3i)(pos.offset / Chunk.Size);
        pos.chunk += chunkDelta;
        pos.offset = pos.offset - chunkDelta * Chunk.Size;
    }
}