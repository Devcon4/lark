using MagicPhysX;

namespace Lark.Engine.physx.managers;

public unsafe class PhysxData {
  public PxFoundation* Foundation;
  public PxPhysics* Physics;
  public PxDefaultCpuDispatcher* Dispatcher;
  public PxSceneDesc SceneDesc;
  public PxScene* Scene;
  public PxControllerManager* ControllerManager;

  public Dictionary<Guid, LarkPhysxMaterial> Materials = [];

  public PxPvd* PVD;
}

public unsafe class LarkPhysxMaterial(PxMaterial* Material) {
  public PxMaterial* Material = Material;
}

public unsafe class LarkPhysxActor(PxActor* Actor) {
  public PxActor* Actor = Actor;
}