using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using nbtsharp;
using Trilateral.World;

namespace Trilateral.World.ChunkGenerators;
public readonly struct ChunkGeneratorRegistryEntry
{
    public ChunkGeneratorRegistryEntry(Func<NBTFolder, IChunkGenerator> inst, IEnumerable<(string, ENBTType)> arg, string id)
    {
        this.Instantiate = inst;
        this.Arguments = arg;
        this.id = id;
    }
    //the NBTFolder contains the settings for the generator.
    public readonly Func<NBTFolder, IChunkGenerator> Instantiate;
    //The arguments the generator expects.
    public readonly IEnumerable<(string, ENBTType)> Arguments;
    public readonly string id;
}