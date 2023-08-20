using System;
using System.IO;
using System.IO.Compression;
using Microsoft.VisualBasic;
using nbtsharp;
using OpenTK.Mathematics;
using Trilateral;
using Trilateral.Utility;
using Trilateral.World;
using Trilateral.World.ChunkGenerators;
using VRenderLib;
using VRenderLib.Interface;
using VRenderLib.Utility;

public sealed class GameWorld : IDisposable
{
    string pathToSaveFolder;
    public readonly ChunkManager chunkManager;
    public WorldPos playerPos;
    public Camera camera;
    public Vector3 playerRotation{get => camera.Rotation; set => camera.Rotation = value;}
    public GameWorld(string pathToSaveFolder, string defaultGeneratorId, float renderThreadsMultiplier, float worldThreadsMultiplier)
    {
        this.pathToSaveFolder = pathToSaveFolder;
        NBTFolder? saveData = null;
        if(File.Exists(pathToSaveFolder + "/save.nbt"))
        {
            try{
                saveData = new NBTFolder(File.ReadAllBytes(pathToSaveFolder + "/save.nbt"));
            }
            catch (Exception e)
            {
                System.Console.Error.WriteLine("Error loading world save: " + e.Message + "\nStakctrace" + e.StackTrace);
            }
        }
        NBTString generatorElement;
        if(saveData is null)
        {
            NBTFloatArr pos = new NBTFloatArr("pos", new float[]{0, 0, 0});
            NBTFloatArr rotation = new NBTFloatArr("rotation", new float[]{0, 0, 0});
            NBTIntArr chunk = new NBTIntArr("chunk", new int[]{0, 2, 0});
            generatorElement = new NBTString("generator", defaultGeneratorId);
            //We don't create the generator settings here since those are initialized by the chunk generator itself.
            saveData = new NBTFolder("save", new INBTElement[]{pos, chunk, rotation, generatorElement});
        }
        //get the generator settings if they exist
        NBTFolder generatorSettings = saveData.GetOrDefault<NBTFolder>("generatorSettings", new NBTFolder("generatorSettings", new INBTElement[0]));
        //Override the generator passed into the constructor with the one already in the save file
        generatorElement = saveData.GetOrDefault<NBTString>("generator", new NBTString("generator", defaultGeneratorId));
        var saveGeneratorId = generatorElement.ContainedString;
        //Get the generator from the id
        IChunkGenerator? generator = null;
        if(Program.Game.ChunkGenerators.TryGetValue(saveGeneratorId, out var generatorEntry))
        {
            generator = generatorEntry.Instantiate(generatorSettings);
        }
        //Couldn't find generator entry or something. try the one given to the constructor
        if(generator is null)
        {
            if(Program.Game.ChunkGenerators.TryGetValue(defaultGeneratorId, out generatorEntry))
                generator = generatorEntry.Instantiate(generatorSettings);
        }
        //still couldn't find it, try just making a simple one
        if(generator is null)
        {
            generator = new BasicChunkGenerator(generatorSettings);
        }
        chunkManager = new ChunkManager(generator, pathToSaveFolder, renderThreadsMultiplier, worldThreadsMultiplier);
        var size = VRender.Render.WindowSize();
        var posArr = saveData.GetOrDefault<NBTFloatArr>("pos", new NBTFloatArr("pos", new float[]{0, 0, 0})).ContainedArray;
        var rotArr = saveData.GetOrDefault<NBTFloatArr>("rotation", new NBTFloatArr("rotation", new float[]{0, 0, 0})).ContainedArray;
        var chunkArr = saveData.GetOrDefault<NBTIntArr>("chunk", new NBTIntArr("chunk", new int[]{0, 2, 0})).ContainedArray;
        playerPos = new WorldPos(new Vector3i(chunkArr[0], chunkArr[1], chunkArr[2]), new Vector3(posArr[0], posArr[1], posArr[2]));
        camera = new Camera(playerPos.offset, new Vector3(rotArr[0], rotArr[1], rotArr[2]), Program.Game.Settings.fieldOfView, size);

    }
    
    public void Dispose()
    {
        var pos = new NBTFloatArr("pos", new float[]{playerPos.offset.X, playerPos.offset.Y, playerPos.offset.Z});
        var rotation = new NBTFloatArr("rotation", new float[]{camera.Rotation.X, camera.Rotation.Y, camera.Rotation.Z});
        var chunk = new NBTIntArr("chunk", new int[]{playerPos.chunk.X, playerPos.chunk.Y, playerPos.chunk.Z});
        var generator = new NBTString("generator", chunkManager.generator.GetId());
        var generatorSettings = chunkManager.generator.GetSettingsNBT("generatorSettings");
        var saveData = new NBTFolder("save", new INBTElement[]{pos, chunk, rotation, generator, generatorSettings});
        File.WriteAllBytes(pathToSaveFolder + "/save.nbt", saveData.Serialize());
        chunkManager.Dispose();
    }

    public void Update()
    {
        Profiler.PushRaw("Update");
        camera.Fovy = Program.Game.Settings.fieldOfView;
        camera.SetAspect(VRender.Render.WindowSize());
        camera.Position = playerPos.offset;
        chunkManager.Update(playerPos.chunk);
        Profiler.PopRaw("Update");
    }

    public void Draw(IDrawCommandQueue drawCommandQueue)
    {
        Profiler.PushRaw("RenderChunks");
        chunkManager.Draw(camera, playerPos.chunk, drawCommandQueue);
        Profiler.PopRaw("RenderChunks");
    }
}