namespace Trilateral;

using OperatingSystemSpecific;
using System.IO;
using System.Collections.Generic;
using vmodel;
using System.Reflection;
using System;

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
            //TODO: add all of the settings here
        }
    }
    //Multiplied by the number of threads on the system to get the actual number of threads to use
    // Requires restart to take effect
    public float worldThreadsMultiplier = 0.75f;
    //Multiplied by the number of threads on the system to get the actual number of threads to use
    // Requires restart to take effect
    public float renderThreadsMultiplier = 0.5f;
    //How far away to load chunks, in meters
    // Takes effect immediately.
    public float loadDistance = 120f;

    //The target frames per second.
    // Takes effect after restart, not because I can't make it dynamic but because i'm too lazy
    public uint targetFPS = 30;

    //Weather to try to sync the framerate with the display.
    // The frame timing code is pretty janky atm, so this might cause strange issues.
    // I suggest leaving it off until I rewrite the timing code to be less weird.
    // requries restart to take effect.
    public bool VSync = false;

    public TimeSpan chunkFlushPeriod = TimeSpan.FromMinutes(1);
}