namespace Trilateral.World.ChunkGenerators;

using World;
using Trilateral.Utility;
using nbtsharp;
using System;

//The "original" world generator for Voxeele-- I mean Trilateral.
// (I'm still getting used to the fact that I renamed it lol)
// Served me well in the beginning
public sealed class BasicChunkGenerator : IChunkGenerator
{
    public IBlock fill;
    public FastNoiseLite noise;

    public BasicChunkGenerator(IBlock fill, FastNoiseLite noise)
    {
        this.fill = fill;
        this.noise = noise;
    }
    public BasicChunkGenerator(NBTFolder settings)
    {
        //fill = settings.fill ?? trilateral:grassBlock ?? VoidBlock
        // (Yes, it's easier to explain in pseudocode than it is in english lol)
        IBlock? fillOrNone = null;
        //Try getting fill from the settings
        if(settings.TryGet<NBTString>("fill", out var fillElement))
        {
            //The settings has the fill parameter, so we use that
            if(!Program.Game.BlockRegistry.TryGetValue(fillElement.ContainedString, out fillOrNone))
            {
                //TryGetValue uses a default value. Idk what the default value for Block is.
                // So, make sure that it's null.
                fillOrNone = null;
            }
        }
        //If getting it from the settings didn't work, try using grass block
        if(fillOrNone is null)
        {
            if(!Program.Game.BlockRegistry.TryGetValue("trilateral:grassBlock", out fillOrNone))
            {
                fillOrNone = null;
            }
        }
        //If none of the above work, as a last resort, just use void block since it's guaranteed to be registered
        fillOrNone ??= Program.Game.AirBlock;
        fill = fillOrNone;

        int? seedOrNone = null;

        if(settings.TryGet<NBTInt>("seed", out var seedElement))
        {
            seedOrNone = seedElement.ContainedInt;
        }
        seedOrNone ??= Random.Shared.Next();

        //TODO: get noise from settings
        noise = new FastNoiseLite(seedOrNone.Value);
        noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise.SetFrequency(0.01f);
        noise.SetFractalOctaves(7);
        noise.SetFractalLacunarity(2.0f);
        noise.SetFractalGain(0.5f);
    }

    public NBTFolder GetSettingsNBT(string folderName)
    {
        return new NBTFolder(folderName,
            new INBTElement[]{
                new NBTString("fill", fill.UUID),
                new NBTInt("seed", noise.GetSeed())
            }
        );
    }
    public Chunk GenerateChunk(int cx, int cy, int cz)
    {
        var chunkWorldPos = MathBits.GetChunkWorldPosUncentered(cx, cy, cz);
        Chunk c = new(new OpenTK.Mathematics.Vector3i(cx, cy, cz), Program.Game.AirBlock);
        for(uint xp = 0; xp < Chunk.Size; xp++){
            for(uint zp = 0; zp < Chunk.Size; zp++){
                var worldPos = MathBits.GetBlockWorldPosLegacy((int)xp, 0, (int)zp) + chunkWorldPos;
                float height = noise.GetNoise(worldPos.X, worldPos.Z);
                height = height*height*100;
                //squaring it makes it better by making lower terrain flatter, and higher terrain more varied and mountain-like
                for(uint yp = 0; yp < Chunk.Size; yp++){
                    worldPos = MathBits.GetBlockWorldPosLegacy((int)xp, (int)yp, (int)zp) + chunkWorldPos;
                    if(worldPos.Y < height) {
                        c.SetBlock(fill, xp, yp, zp);
                    }
                }
            }
        }
        return c;
    }

    public static ChunkGeneratorRegistryEntry CreateEntry()
    {
        return new ChunkGeneratorRegistryEntry(
            (s) => new BasicChunkGenerator(s),
            new (string, ENBTType)[]{("fill", ENBTType.String), ("seed", ENBTType.Int)},
            id
        );
    }
    private const string id = "trilateral:basic";
    public string GetId() {return id;}
}