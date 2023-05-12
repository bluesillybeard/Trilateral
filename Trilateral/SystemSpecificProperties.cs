namespace Trilateral.OperatingSystemSpecific;

using System;

public class StaticProperties
{
    public readonly string pathToConfig;
    public readonly Version gameVersion;
    public StaticProperties()
    {
        gameVersion = new Version(0, 0, 1);
        //Different versions of the game have different config folders.
        // TODO: in the future, make a GUI to migrate data between different versions of the game.
        //LINUX, MAC, BSD, etc:
        // TODO: verify that Mac and BSD actually use the same properties that Linux does
        if(System.Environment.OSVersion.Platform == System.PlatformID.Unix)
        {
            pathToConfig = "/home/" + Environment.UserName +  "/.config/Trilateral/" + gameVersion.ToString(3) + "/";
            // the default places is in ~/.config/[application]
            // Some applications put it in ~/.local/share/[application] or ~/.[application]
            // .local is a big mess that I don't even want to touch,
            // putting it directly in the home directory is generally frowned upon,
            // So, ~/.config is the most reasonable place to put it.
        }
            else if(System.Environment.OSVersion.Platform == System.PlatformID.Win32NT)
        {
            //TODO (VERY VERY VERY VERY VERY IMPORTANT) TEST THIS TO MAKE SURE IT WORKS
            pathToConfig = "%appdata%/Roaming/Trilateral/" + gameVersion.ToString(3) + "/";
            //If you thought Linux was a mess, Windows is even worse.
            // There are at least 6 differet places that applications can use to store settings, including:
            // [home]/Documents/[application]
            // [home]/AppData/Roaming/[application]
            // [home]/AppData/Local/[application]
            // [home]/AppData/LocalLow/[application]
            // [home]/Saved Games
            // [the registry]/[application]
            // where the application is installed (the worst of them all, at least for larger games)
        }
            else 
        {
            throw new System.Exception("SYSTEM IS NOT SUPPORTED");
        }
    }


}