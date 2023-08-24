namespace Trilateral;

using System;
using System.IO;
using System.Runtime.Intrinsics.X86;
using nbtsharp;
using Trilateral.Utility;
public struct Settings
{
    readonly string filePath;
    public Settings(StaticProperties properties)
    {
        filePath = properties.pathToConfig + "settings.nbt";
        NBTFolder settingsData;
        if(File.Exists(filePath))
        {
            settingsData = new NBTFolder(File.ReadAllBytes(filePath));
        }
        else
        {
            settingsData = new NBTFolder("settings");
        }

        //read in settings/apply defaults
        maxChunkUpdatesPerFrame = settingsData.Get<NBTInt>("maxChunkUpdatesPerFrame")?.ContainedInt ?? 100;
        horizontalLoadDistance = settingsData.Get<NBTFloat>("horizontalLoadDistance")?.ContainedFloat ?? 200;
        verticalLoadDistance = settingsData.Get<NBTFloat>("verticalLoadDistance")?.ContainedFloat ?? 150;
        targetFPS = settingsData.Get<NBTUInt>("targetFPS")?.ContainedUint ?? 60;
        VSync = (settingsData.Get<NBTInt>("VSync")?.ContainedInt ?? 0) == 1;
        fieldOfView = settingsData.Get<NBTFloat>("fieldOfView")?.ContainedFloat ?? 200;
        chunkFlushPeriod = TimeSpan.FromTicks(settingsData.Get<NBTUInt>("chunkFlushPeriod")?.ContainedUint ?? TimeSpan.TicksPerMinute*5);
    }
    public readonly void Flush()
    {
        var settingsData = new NBTFolder("settings", new INBTElement[]{
            new NBTInt("maxChunkUpdatesPerFrame", maxChunkUpdatesPerFrame),
            new NBTFloat("horizontalLoadDistance", horizontalLoadDistance),
            new NBTFloat("verticalLoadDistance", verticalLoadDistance),
            new NBTUInt("targetFPS", targetFPS),
            new NBTInt("VSync", VSync ? 1 : 0),
            new NBTFloat("fieldOfView", fieldOfView),
            new NBTUInt("chunkFlushPeriod", (uint)chunkFlushPeriod.Ticks),
        });
        File.WriteAllBytes(filePath, settingsData.Serialize());
    }
    // The maximum number of chunks that can be uploaded each frame
    public int maxChunkUpdatesPerFrame;

    //How far away to load chunks, in meters
    // Takes effect immediately.
    public float horizontalLoadDistance;
    public float verticalLoadDistance;

    //The target frames per second.
    // Takes effect after restart, not because I can't make it dynamic but because i'm too lazy
    public uint targetFPS;

    //Weather to try to sync the framerate with the display.
    // The frame timing code is pretty janky atm, so this might cause strange issues.
    // I suggest leaving it off until I rewrite the timing code to be less weird.
    // requries restart to take effect.
    public bool VSync;

    public float fieldOfView;

    public TimeSpan chunkFlushPeriod;
}
