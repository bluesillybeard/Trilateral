namespace Trilateral;

using OperatingSystemSpecific;
using System.IO;
using System.Collections.Generic;
using vmodel;
using System.Reflection;

public struct Settings
{
    public Settings(StaticProperties properties)
    {
        if(File.Exists(properties.pathToConfig + "config"))
        {
            //If the file exists, read it in.
            // We'll take advantage of a method that VModel uses, since what we're reading has the same syntax.
            string file = File.ReadAllText(properties.pathToConfig + "config");
            Dictionary<string, string> items = VModelUtils.ParseListMap(file, out var errors);
            if(errors is not null)
            {
                System.Console.Error.WriteLine("Errors encountered loading settings:" + string.Join(", ", errors));
            }
            //For the sake of making adding new settings as easy as possible,
            // use Reflection to set the values
            var settingsType = typeof(Settings);
            foreach(var item in items)
            {
                var field = settingsType.GetRuntimeField(item.Key);
                if(field is null)
                {
                    System.Console.Error.WriteLine("ERROR: Skipping setting field " + item.Key);
                    continue;
                }
                
            }
        }
    }
    //Multiplied by the number of threads on the system to get the actual number of threads to use
    // Requires restart to take effect
    public float worldThreadsMultiplier = 0.75f;
    //Multiplied by the number of threads on the system to get the actual number of threads to use
    // Requires restart to take effect
    public float renderThreadsMultiplier = 0.75f;
    //How far away to load chunks, in meters
    // Takes effect immediately.
    public float loadDistance = 150f;

    //The target frames per second.
    // Takes effect immediately
    public uint targetFPS = 60;

    //Weather to try to sync the framerate with the display.
    // The frame timing code is somewhat messed up right now, so this might cause strange performance issues.
    // requries restart to take effect.
    public bool VSync = false;
}