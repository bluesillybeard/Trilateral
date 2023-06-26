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
using VRenderLib.Utility;

public sealed class GameWorld : IDisposable
{
    string pathToSaveFolder;
    public readonly ChunkManager chunkManager;
    //We keep the camera position small, and use a chunk's position.
    public Vector3i playerChunk;
    public Camera camera;
    public GameWorld(string pathToSaveFolder, string generatorId, float renderThreadsMultiplier, float worldThreadsMultiplier)
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
            generatorElement = new NBTString("generator", generatorId);
            //We don't create the generator settings here since those are initialized by the chunk generator itself.
            saveData = new NBTFolder("save", new INBTElement[]{pos, chunk, rotation, generatorElement});
        }
        //get the generator settings if they exist
        NBTFolder generatorSettings = saveData.GetOrDefault<NBTFolder>("generatorSettings", new NBTFolder("generatorSettings", new INBTElement[0]));
        //Override the generator passed into the constructor with the one already in the save file
        generatorElement = saveData.GetOrDefault<NBTString>("generator", new NBTString("generator", generatorId));
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
            if(Program.Game.ChunkGenerators.TryGetValue(generatorId, out generatorEntry))
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
        camera = new Camera(new Vector3(posArr[0], posArr[1], posArr[2]), new Vector3(rotArr[0], rotArr[1], rotArr[2]), Program.Game.Settings.fieldOfView, size);
        var chunkArr = saveData.GetOrDefault<NBTIntArr>("chunk", new NBTIntArr("chunk", new int[]{0, 2, 0})).ContainedArray;
        playerChunk = new Vector3i(chunkArr[0], chunkArr[1], chunkArr[2]);
    }
    
    public void Dispose()
    {
        var pos = new NBTFloatArr("pos", new float[]{camera.Position.X, camera.Position.Y, camera.Position.Z});
        var rotation = new NBTFloatArr("rotation", new float[]{camera.Rotation.X, camera.Rotation.Y, camera.Rotation.Z});
        var chunk = new NBTIntArr("chunk", new int[]{playerChunk.X, playerChunk.Y, playerChunk.Z});
        var generator = new NBTString("generator", chunkManager.generator.GetId());
        var generatorSettings = chunkManager.generator.GetSettingsNBT("generatorSettings");
        var saveData = new NBTFolder("save", new INBTElement[]{pos, chunk, rotation, generator, generatorSettings});
        File.WriteAllBytes(pathToSaveFolder + "/save.nbt", saveData.Serialize());
        chunkManager.Dispose();
    }

    public void Update()
    {
        camera.Fovy = Program.Game.Settings.fieldOfView;
        Profiler.PushRaw("UpdateChunks");
        chunkManager.Update(playerChunk);
        Profiler.PopRaw("UpdateChunks");
    }

    public void Draw()
    {
        Profiler.PushRaw("RenderChunks");
        chunkManager.Draw(camera, playerChunk);
        Profiler.PopRaw("RenderChunks");
    }
}