using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;

namespace Trilateral.Physics;

//Bepuphysics demends a handful of callback things.
// These are generally quite useful, although I haven't really made good use of them.
public unsafe struct TrilateralNarrowPhaseCallbacks : INarrowPhaseCallbacks
{
    public readonly void Initialize(Simulation simulation)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
    {
        //While the engine won't even try creating pairs between statics at all, it will ask about kinematic-kinematic pairs.
        //Those pairs cannot emit constraints since both involved bodies have infinite inertia. Since most of the demos don't need
        //to collect information about kinematic-kinematic pairs, we'll require that at least one of the bodies needs to be dynamic.
        return a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
    {
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        //TODO: store these somewhere actually useful
        pairMaterial.FrictionCoefficient = 1;
        pairMaterial.MaximumRecoveryVelocity = 200;
        //TODO: find a way to get the frequency of the simulation and divide it by 3 instead of using magic numbers
        pairMaterial.SpringSettings = new SpringSettings(10, 50);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold)
    {
        return true;
    }

    public readonly void Dispose()
    {
    }
}


public struct TrilateralPoseIntegratorCallbacks: IPoseIntegratorCallbacks
{
    public readonly AngularIntegrationMode AngularIntegrationMode { get => AngularIntegrationMode.Nonconserving; }
    public readonly bool AllowSubstepsForUnconstrainedBodies { get => false; }
    public readonly bool IntegrateVelocityForKinematics { get => false; }
    public readonly void Initialize(Simulation simulation){}
    public readonly void PrepareForIntegration(float dt){
    }
    public readonly void IntegrateVelocity(
        Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation, BodyInertiaWide localInertia,
        Vector<int> integrationMask, int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity)
    {
    }
}