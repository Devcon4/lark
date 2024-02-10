using System.Numerics;
using Lark.Engine.ecs;
using Lark.Engine.physx.components;
using Lark.Engine.std;
using Microsoft.Extensions.Logging;
using MagicPhysX;
using static MagicPhysX.NativeMethods;
using Microsoft.Extensions.Options;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using Serilog.Context;

namespace Lark.Engine.physx.managers;

public record struct PhysxEntityMarker : ILarkComponent { }

public class PhysxManager(ILogger<PhysxManager> logger, EntityManager em, TimeManager tm, PhysxData physxData, IOptions<LarkPhysxConfig> options) {
  public static Type[] PhysxWorldEntity => [typeof(SystemComponent), typeof(PhysxEntityMarker)];

  public Dictionary<Guid, Guid> EntityToActor = [];

  public Dictionary<Guid, LarkPhysxActor> ActorLookup = [];

  public Guid DefaultMaterialId { get; private set; }

  public bool HasActor(Guid entityId) {
    return EntityToActor.ContainsKey(entityId);
  }

  public Guid GetActorId(Guid entityId) {
    if (!EntityToActor.TryGetValue(entityId, out var actorId)) {
      throw new Exception($"Entity {entityId} does not have an actor");
    }

    return actorId;
  }

  public void SetActorId(Guid entityId, Guid actorId) {
    if (EntityToActor.ContainsKey(entityId)) {
      throw new Exception($"Entity {entityId} already has an actor");
    }

    EntityToActor.Add(entityId, actorId);
  }

  private readonly object physxLock = new();
  private readonly float timestep = 1.0f / 60.0f; // 60 FPS
  private float accumulator = 0.0f;

  private float physicsFrames = 0.0f;
  private float physicsFPSAccumulator = 0.0f;

  IDisposable? PFPS;
  public unsafe void SimulateFrame() {
    // return if no scene
    if (physxData.Scene is null) {
      return;
    }

    physicsFrames++;
    // calculate physics fps
    physicsFPSAccumulator += (float)tm.DeltaTime.TotalMilliseconds;
    if (physicsFPSAccumulator >= 1000.0f) {
      PFPS?.Dispose();
      PFPS = LogContext.PushProperty("PFPS", physicsFrames);
      physicsFPSAccumulator = 0.0f;
      physicsFrames = 0.0f;
    }

    lock (physxLock) {
      var frameTime = (float)Math.Clamp(tm.DeltaTime.TotalMilliseconds / 1000.0f, 0, 0.25f); // convert to seconds
      accumulator += frameTime;

      var stepCounter = 0;
      while (accumulator >= timestep) {
        physxData.Scene->SimulateMut(timestep, null, null, 0, true);
        accumulator -= timestep;
        stepCounter++;

        uint errorState = 0;
        physxData.Scene->FetchResultsMut(true, &errorState);

        if (errorState != 0) {
          logger.LogError("Physx :: Error state {errorState}", errorState);
        }
      }
    }
  }

  // AddForce
  public unsafe void AddForce(Guid actorId, Vector3 force, PxForceMode forceMode) {
    if (!ActorLookup.TryGetValue(actorId, out var actor)) {
      throw new Exception($"Actor {actorId} does not exist");
    }

    var rigidbody = (PxRigidBody*)actor.Actor;
    var f = new PxVec3 { x = force.X, y = force.Y, z = force.Z };
    PxRigidBody_addForce_mut(rigidbody, &f, forceMode, true);
  }

  public unsafe void AddTorque(Guid actorId, Vector3 torque, PxForceMode forceMode) {
    if (!ActorLookup.TryGetValue(actorId, out var actor)) {
      throw new Exception($"Actor {actorId} does not exist");
    }

    var rigidbody = (PxRigidBody*)actor.Actor;
    var t = new PxVec3 { x = torque.X, y = torque.Y, z = torque.Z };
    PxRigidBody_addTorque_mut(rigidbody, &t, forceMode, true);
  }

  public void BuildPhysxWorld(string worldName) {
    logger.LogInformation("Physx :: Building world {worldName}", worldName);
    var (id, components) = em.GetEntity(PhysxWorldEntity);
    PhysxWorldComponent? existing = components.GetList<PhysxWorldComponent>().FirstOrDefault(c => c.WorldName == worldName);

    if (existing is null) {
      throw new Exception($"World {worldName} does not exist");
    }

    CreateFoundation();
    CreateScene();
    SetGravity(existing.Value.Gravity);
    CreateControllerManager();

    // Register default material
    DefaultMaterialId = Guid.NewGuid();
    RegisterMaterial(DefaultMaterialId, 0.5f, 0.5f, 0.6f);
  }

  public unsafe void CreateControllerManager() {
    logger.LogInformation("Physx :: Creating CCT manager");
    physxData.ControllerManager = phys_PxCreateControllerManager(physxData.Scene, false);
  }

  public bool HasMaterial(Guid materialId) {
    return physxData.Materials.ContainsKey(materialId);
  }
  public unsafe void RegisterMaterial(Guid materialId, float staticFriction, float dynamicFriction, float restitution) {
    logger.LogInformation("Physx :: Registering material {materialName}", materialId);

    if (HasMaterial(materialId)) {
      var existing = physxData.Materials[materialId];
      existing.Material->SetStaticFrictionMut(staticFriction);
      existing.Material->SetDynamicFrictionMut(dynamicFriction);
      existing.Material->SetRestitutionMut(restitution);
      return;
    }

    var material = physxData.Physics->CreateMaterialMut(staticFriction, dynamicFriction, restitution);
    physxData.Materials.Add(materialId, new(material));
  }

  public LarkPhysxMaterial GetMaterial(Guid materialId) {
    if (!HasMaterial(materialId)) {
      throw new Exception($"Material {materialId} does not exist");
    }

    return physxData.Materials[materialId];
  }


  public unsafe void DeleteActor(Guid actorId) {
    logger.LogInformation("Physx :: Deleting actor {actorId}", actorId);
    if (!ActorLookup.TryGetValue(actorId, out var actor)) {
      throw new Exception($"Actor {actorId} does not exist");
    }

    physxData.Scene->RemoveActorMut(actor.Actor, true);
    PxRigidActor_release_mut((PxRigidActor*)actor.Actor);
    ActorLookup.Remove(actorId);
    EntityToActor.Remove(actorId);
  }

  public unsafe ValueTuple<Vector3, Quaternion> GetActorTransform(Guid actorId) {
    if (!ActorLookup.TryGetValue(actorId, out var actor)) {
      throw new Exception($"Actor {actorId} does not exist");
    }

    lock (physxLock) {
      var transform = PxRigidActor_getGlobalPose((PxRigidActor*)actor.Actor);
      var Translation = new Vector3(transform.p.x, transform.p.y, transform.p.z);
      var Rotation = new Quaternion(transform.q.x, transform.q.y, transform.q.z, transform.q.w);
      return new(Translation, Rotation);
    }
  }

  public unsafe void UpdateRigidbody(Guid actorId, float mass, float linearDamping, float angularDamping, bool isKinematic) {
    if (!ActorLookup.TryGetValue(actorId, out var actor)) {
      throw new Exception($"Actor {actorId} does not exist");
    }

    lock (physxLock) {
      var rigidbody = (PxRigidBody*)actor.Actor;
      PxRigidBody_setRigidBodyFlag_mut(rigidbody, PxRigidBodyFlag.Kinematic, isKinematic);

      PxRigidBody_setMass_mut(rigidbody, mass);
      PxRigidBody_setLinearDamping_mut(rigidbody, linearDamping);
      PxRigidBody_setAngularDamping_mut(rigidbody, angularDamping);
    }
  }

  public unsafe ValueTuple<float, float, float, bool> GetRigidbody(Guid actorId) {
    if (!ActorLookup.TryGetValue(actorId, out var actor)) {
      throw new Exception($"Actor {actorId} does not exist");
    }

    var rigidbody = (PxRigidBody*)actor.Actor;
    var mass = PxRigidBody_getMass(rigidbody);
    var linearDamping = PxRigidBody_getLinearDamping(rigidbody);
    var angularDamping = PxRigidBody_getAngularDamping(rigidbody);
    var isKinematic = PxRigidBody_getRigidBodyFlags(rigidbody).HasFlag(PxRigidBodyFlag.Kinematic);
    return new(mass, linearDamping, angularDamping, isKinematic);
  }

  private unsafe void SetPVD() {
    if (!options.Value.PVDEnable) {
      logger.LogInformation("Physx :: PVD is disabled");
      return;
    }

    var pvdPort = options.Value.PVDPort ?? 5425;
    logger.LogInformation("Physx :: Enabling PVD at {pvdHost}:{pvdPort}", options.Value.PVDHost, pvdPort);
    var pvd = phys_PxCreatePvd(physxData.Foundation);
    // pvdHost as a byte pointer
    var pvdHost = (byte*)Marshal.StringToHGlobalAnsi(options.Value.PVDHost);

    var transport = phys_PxDefaultPvdSocketTransportCreate(pvdHost, pvdPort, 10);
    pvd->ConnectMut(transport, PxPvdInstrumentationFlags.All);

    physxData.PVD = pvd;
  }

  private unsafe void CreateFoundation() {
    logger.LogInformation("Physx :: Creating foundation");
    physxData.Foundation = physx_create_foundation();
    SetPVD();

    uint PX_PHYSICS_VERSION_MAJOR = 5;
    uint PX_PHYSICS_VERSION_MINOR = 1;
    uint PX_PHYSICS_VERSION_BUGFIX = 3;
    uint versionNumber = (PX_PHYSICS_VERSION_MAJOR << 24) + (PX_PHYSICS_VERSION_MINOR << 16) + (PX_PHYSICS_VERSION_BUGFIX << 8);

    var tolerancesScale = new PxTolerancesScale { length = 1, speed = 10 };

    physxData.Physics = phys_PxCreatePhysics(versionNumber, physxData.Foundation, &tolerancesScale, true, physxData.PVD, null);
    phys_PxInitExtensions(physxData.Physics, physxData.PVD);


    var sceneDesc = PxSceneDesc_new(PxPhysics_getTolerancesScale(physxData.Physics));
    var dispatcher = phys_PxDefaultCpuDispatcherCreate(4, null, PxDefaultCpuDispatcherWaitForWorkMode.WaitForWork, 0);
    sceneDesc.cpuDispatcher = (PxCpuDispatcher*)dispatcher;
    sceneDesc.filterShader = get_default_simulation_filter_shader();

    physxData.Dispatcher = dispatcher;
    physxData.SceneDesc = sceneDesc;
  }

  public unsafe void SetGravity(Vector3 gravity) {

    logger.LogInformation("Physx :: Setting gravity to {gravity}", gravity);

    if (physxData.Scene is null) {
      throw new Exception("Scene is null, make sure to call CreateScene first");
    }

    var g = new PxVec3 { x = gravity.X, y = gravity.Y, z = gravity.Z };
    physxData.Scene->SetGravityMut(&g);
  }

  public unsafe Vector3 GetGravity() {
    if (physxData.Scene is null) {
      throw new Exception("Scene is null, make sure to call CreateScene first");
    }

    var g = physxData.Scene->GetGravity();
    return new Vector3((float)g.x, (float)g.y, (float)g.z);
  }

  private unsafe void CreateScene() {
    logger.LogInformation("Physx :: Creating scene");
    fixed (PxSceneDesc* sceneDesc = &physxData.SceneDesc) {
      var scene = physxData.Physics->CreateSceneMut(sceneDesc);
      physxData.Scene = scene;
    }

    if (options.Value.PVDEnable) {
      // pvd client
      var pvdClient = physxData.Scene->GetScenePvdClientMut();
      if (pvdClient != null) {
        pvdClient->SetScenePvdFlagMut(PxPvdSceneFlag.TransmitConstraints, true);
        pvdClient->SetScenePvdFlagMut(PxPvdSceneFlag.TransmitContacts, true);
        pvdClient->SetScenePvdFlagMut(PxPvdSceneFlag.TransmitScenequeries, true);
      }
    }
  }

  public unsafe void Cleanup() {
    logger.LogInformation("Physx :: Disposing");

    foreach (var actor in ActorLookup.Values) {
      PxRigidActor_release_mut((PxRigidActor*)actor.Actor);
    }

    if (physxData.PVD != null) {
      physxData.PVD->DisconnectMut();
      physxData.PVD->ReleaseMut();
    }

    physxData.Materials.Clear();
    PxScene_release_mut(physxData.Scene);
    PxDefaultCpuDispatcher_release_mut(physxData.Dispatcher);
    PxPhysics_release_mut(physxData.Physics);
    PxFoundation_release_mut(physxData.Foundation);
  }

  public unsafe void UpdateActorTransform(Guid actorId, Vector3 position, Quaternion rotation) {
    if (!ActorLookup.TryGetValue(actorId, out var actor)) {
      throw new Exception($"Actor {actorId} does not exist");
    }

    var p = new PxVec3 { x = position.X, y = position.Y, z = position.Z };
    var q = new PxQuat { x = rotation.X, y = rotation.Y, z = rotation.Z, w = rotation.W };

    lock (physxLock) {
      var transform = PxTransform_new_5(&p, &q);
      PxRigidActor_setGlobalPose_mut((PxRigidActor*)actor.Actor, &transform, true);
    }
  }
}
