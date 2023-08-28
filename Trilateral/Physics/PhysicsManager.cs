namespace Trilateral.Physics;

using System;
using BepuPhysics;
using BepuUtilities.Memory;

public sealed class PhysicsManager: IDisposable
{
    readonly BufferPool pool;
    public PhysicsManager()
    {
        pool = new BufferPool();
        //TODO: there is A LOT of tuning that can be done here.
        // Trilateral has a large timestep, so stability might become a problem. I say might, because i really dont know
        Sim = Simulation.Create(pool, new TrilateralNarrowPhaseCallbacks(), new TrilateralPoseIntegratorCallbacks(), new SolveDescription(1, 1));
        Sim.Deterministic = false;
    }
    public Simulation Sim {get; private set;}

    public void RunPhysics(TimeSpan delta)
    {
        Sim.Timestep(((float)delta.Ticks) / TimeSpan.TicksPerSecond);
    }

    public void Dispose()
    {
        Sim.Dispose();
    }
}