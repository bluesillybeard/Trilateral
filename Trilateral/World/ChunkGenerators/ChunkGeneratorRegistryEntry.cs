using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using nbtsharp;
using Trilateral.World;
using Trilateral.World.ChunkGenerators;

public struct ChunkGeneratorRegistryEntry
{
    public ChunkGeneratorRegistryEntry(Func<NBTFolder, IChunkGenerator> inst, IEnumerable<(string, ENBTType)> arg)
    {
        this.Instantiate = inst;
        this.Arguments = arg;
    }
    //the NBTFolder contains the settings for the generator.
    public readonly Func<NBTFolder, IChunkGenerator> Instantiate;
    //The arguments the generator expects.
    public readonly IEnumerable<(string, ENBTType)> Arguments;
}