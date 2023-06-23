using System;
using System.IO;
using System.IO.Compression;
using nbtsharp;
using OpenTK.Mathematics;
using Trilateral;
using Trilateral.Utility;
using Trilateral.World;
using VRenderLib;
using VRenderLib.Utility;

public sealed class GameWorld : IDisposable
{
    string pathToSaveFolder;
    public readonly ChunkManager chunkManager;
    //We keep the camera position small, and use a chunk's position.
    public Vector3i playerChunk;
    public Camera camera;
    public GameWorld(string pathToSaveFolder, IChunkGenerator generator, float renderThreadsMultiplier, float worldThreadsMultiplier)
    {
        NBTFolder saveData;
        if(File.Exists(pathToSaveFolder + "/save.nbt"))
        {
            saveData = new NBTFolder(File.ReadAllBytes(pathToSaveFolder + "/save.nbt"));
        } else 
        {
            NBTFloatArr pos = new NBTFloatArr("pos", new float[]{0, 0, 0});
            NBTFloatArr rotation = new NBTFloatArr("rotation", new float[]{0, 0, 0});
            NBTIntArr chunk = new NBTIntArr("chunk", new int[]{0, 2, 0});
            saveData = new NBTFolder("save", new INBTElement[]{pos, chunk, rotation});
        }
        chunkManager = new ChunkManager(generator, pathToSaveFolder, renderThreadsMultiplier, worldThreadsMultiplier);
        var size = VRender.Render.WindowSize();
        var posArr = saveData.GetOrDefault<NBTFloatArr>("pos", new NBTFloatArr("pos", new float[]{0, 0, 0})).ContainedArray;
        var rotArr = saveData.GetOrDefault<NBTFloatArr>("rotation", new NBTFloatArr("rotation", new float[]{0, 0, 0})).ContainedArray;
        camera = new Camera(new Vector3(posArr[0], posArr[1], posArr[2]), new Vector3(rotArr[0], rotArr[1], rotArr[2]), Program.Game.Settings.fieldOfView, size);
        var chunkArr = saveData.GetOrDefault<NBTIntArr>("chunk", new NBTIntArr("chunk", new int[]{0, 2, 0})).ContainedArray;
        playerChunk = new Vector3i(chunkArr[0], chunkArr[1], chunkArr[2]);
        this.pathToSaveFolder = pathToSaveFolder;
    }
    
    public void Dispose()
    {
        chunkManager.Dispose();
        var pos = new NBTFloatArr("pos", new float[]{camera.Position.X, camera.Position.Y, camera.Position.Z});
        var rotation = new NBTFloatArr("rotation", new float[]{camera.Rotation.X, camera.Rotation.Y, camera.Rotation.Z});
        var chunk = new NBTIntArr("chunk", new int[]{playerChunk.X, playerChunk.Y, playerChunk.Z});
        var saveData = new NBTFolder("save", new INBTElement[]{pos, chunk, rotation});
        File.WriteAllBytes(pathToSaveFolder + "/save.nbt", saveData.Serialize());
    }

    public void Update()
    {
        camera.Fovy = Program.Game.Settings.fieldOfView;
        Profiler.PushRaw("UpdateChunks");
        chunkManager.Update(playerChunk, Program.Game.Settings.loadDistance);
        Profiler.PopRaw("UpdateChunks");
    }

    public void Draw()
    {
        Profiler.PushRaw("RenderChunks");
        chunkManager.Draw(camera, playerChunk);
        Profiler.PopRaw("RenderChunks");
    }
}