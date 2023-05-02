namespace Trilateral;

public struct Settings
{
    public Settings(float worldThreadsMultiplier, float renderThreadsMultiplier, float loadDistance, uint targetFPS, bool VSync)
    {
        this.worldThreadsMultiplier = worldThreadsMultiplier;
        this.renderThreadsMultiplier = renderThreadsMultiplier;
        this.loadDistance = loadDistance;
        this.targetFPS = targetFPS;
        this.VSync = VSync;
    }

    //Default settings
    public Settings()
    {

    }
    //Multiplied by the number of threads on the system to get the actual number of threads to use
    // Requires restart to take effect
    public float worldThreadsMultiplier = 0.75f;
    //Multiplied by the number of threads on the system to get the actual number of threads to use
    // Requires restart to take effect
    public float renderThreadsMultiplier = 0.75f;
    //How far away to load chunks, in meters
    // Takes effect immediately.
    public float loadDistance = 60f;

    //The target frames per second.
    // Takes effect immediately
    public uint targetFPS = 100;

    //Weather to try to sync the framerate with the display.
    // The frame timing code is somewhat messed up right now, so this might cause strange performance issues.
    // requries restart to take effect.
    public bool VSync = false;

}