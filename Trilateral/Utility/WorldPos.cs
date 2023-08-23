using OpenTK.Mathematics;

namespace Trilateral.Utility
{
    public struct WorldPos
    {
        public WorldPos(Vector3i chunk, Vector3 offset)
        {
            this.chunk = chunk;
            this.offset = offset;
        }
        public Vector3i chunk;
        public Vector3 offset;

        public override readonly bool Equals(object? obj)
        {
            return obj is WorldPos pos && pos.chunk == chunk && pos.offset == offset;
        }
        public override int GetHashCode()
        {
            return chunk.GetHashCode() * offset.GetHashCode();
        }

        public override string ToString()
        {
            return chunk.ToString() + "|" + offset.ToString();
        }

        public static bool operator ==(WorldPos a, WorldPos b)
        {
            return a.chunk == b.chunk && a.offset == b.offset;
        }
        public static bool operator !=(WorldPos a, WorldPos b)
        {
            return a.chunk != b.chunk || a.offset != b.offset;
        }

        public static WorldPos operator +(WorldPos a, WorldPos b)
        {
            WorldPos output = new()
            {
                chunk = a.chunk + b.chunk,
                offset = a.offset + b.offset
            };
            RestoreNormality(ref output);
            return output;
        }

        public static WorldPos operator -(WorldPos a, WorldPos b)
        {
            WorldPos output = new()
            {
                chunk = a.chunk - b.chunk,
                offset = a.offset - b.offset
            };
            RestoreNormality(ref output);
            return output;
        }

        //TODO: dot and cross products, since WorldPos is basically just a fancy way to store a vector
        public static void RestoreNormality(ref WorldPos pos)
        {
            Vector3i chunkDelta = MathBits.GetChunkPos(pos.offset);
            pos.chunk += chunkDelta;
            pos.offset -= MathBits.GetChunkWorldPosUncentered(chunkDelta);
        }

        //TODO: at some point, all references to this should be gone and it should be able to be removed.
        // In an ideal world. WorldPos should be a drop-in replacement for Vector3.
        public readonly Vector3 LegacyValue => offset + MathBits.GetChunkWorldPosUncentered(chunk.X, chunk.Y, chunk.Z);
    }
}