namespace Trilateral.Utility;

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
            var config = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            pathToConfig = config + "/Trilateral/" + gameVersion.ToString(3) + "/";
            System.Console.WriteLine("Putting trilateral save data in \"" + pathToConfig + "\"");
            // the default places is in ~/.config/[application]
            // Some applications put it in ~/.local/share/[application] or ~/.[application]
            // .local is a big mess that I don't even want to touch,
            // putting it directly in the home directory is generally frowned upon,
            // So, ~/.config is the most reasonable place to put it.
            // Thankfully, C# developers had the same thought process as me, because SpecialVolder.ApplicationData leads exactly to that.
        }
            else if(System.Environment.OSVersion.Platform == System.PlatformID.Win32NT)
        {
            var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            //TODO (VERY VERY VERY VERY VERY IMPORTANT) TEST THIS TO MAKE SURE IT WORKS
            pathToConfig = appdata + "/Roaming/Trilateral/" + gameVersion.ToString(3) + "/";
            System.Console.WriteLine("Putting trilateral save data in \"" + pathToConfig + "\"");
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