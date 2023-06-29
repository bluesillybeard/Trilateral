namespace Trilateral.World.ChunkGenerators;

using World;
using Trilateral.Utility;
using nbtsharp;
using System;

//The "original" world generator for Voxeele-- I mean Trilateral.
// (I'm still getting used to the fact that I renamed it lol)
// Served me well in the beginning
public class BasicChunkGenerator : IChunkGenerator
{
    public Block fill;
    public FastNoiseLite noise;

    public BasicChunkGenerator(Block fill, FastNoiseLite noise)
    {
        this.fill = fill;
        this.noise = noise;
    }
    public BasicChunkGenerator(NBTFolder settings)
    {
        //fill = settings.fill ?? trilateral:grassBlock ?? VoidBlock
        // (Yes, it's easier to explain in pseudocode than it is in english lol)
        Block? fillOrNone = null;
        //Try getting fill from the settings
        if(settings.TryGet<NBTString>("fill", out var fillElement))
        {
            //The settings has the fill parameter, so we use that
            if(!Program.Game.BlockRegistry.TryGetValue(fillElement.ContainedString, out fillOrNone))
            {
                //TryGetValue uses a default value. Idk what the default value for Block is.
                // So, I make sure that it's null.
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
        if(fillOrNone is null)
        {
            fillOrNone = Program.Game.VoidBlock;
        }
        fill = fillOrNone;

        int? seedOrNone = null;

        if(settings.TryGet<NBTInt>("seed", out var seedElement))
        {
            seedOrNone = seedElement.ContainedInt;
        }
        if(seedOrNone is null)
        {
            seedOrNone = Random.Shared.Next();
        }

        //TODO: get noise from settings
        noise = new FastNoiseLite(seedOrNone.Value);
        noise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
        noise.SetFractalType(FastNoiseLite.FractalType.Ridged);
        noise.SetFrequency(0.004f);
        noise.SetFractalOctaves(5);
        noise.SetFractalLacunarity(2.0f);
        noise.SetFractalGain(0.5f);
    }

    public NBTFolder GetSettingsNBT(string folderName)
    {
        return new NBTFolder(folderName, 
            new INBTElement[]{
                new NBTString("fill", fill.uid),
                new NBTInt("seed", noise.GetSeed())
            }
        );
    }
    public Chunk GenerateChunk(int cx, int cy, int cz)
    {
        int csy = (int) (Chunk.Size * cy);
        int csx = (int) (Chunk.Size * cx);
        int csz = (int) (Chunk.Size * cz * 0.5773502692f);
        Chunk c = new Chunk(new OpenTK.Mathematics.Vector3i(cx, cy, cz), Program.Game.VoidBlock);
        for(uint xp = 0; xp < Chunk.Size; xp++){
            for(uint zp = 0; zp < Chunk.Size; zp++){
                float height = noise.GetNoise(csx+xp, csz+(zp*0.5773502692f));
                height = height*height*40;
                //squaring it makes it better by making lower terrain flatter, and higher terrain more varied and mountain-like
                for(uint yp = 0; yp < Chunk.Size; yp++){
                    if(csy+yp < height) {
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
            (s)=>{return new BasicChunkGenerator(s);},
            new (string, ENBTType)[]{("fill", ENBTType.String), ("seed", ENBTType.Int)},
            id
        );
    }
    private const string id = "trilateral:basic";
    public string GetId() {return id;}
}