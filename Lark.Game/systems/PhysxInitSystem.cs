using System.Collections.Frozen;
using System.Numerics;
using Lark.Engine.ecs;
using Lark.Engine.physx.managers;
using Lark.Engine.physx.systems;
using Lark.Engine.std;
using Lark.Game.managers;
using MagicPhysX;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Lark.Game.systems;

// Testing system which creates a plane and a cube and drops the cube onto the plane. The cube should use the antique camera mesh.
public class PhysxInitSystem(EntityManager em, PhysxManager pm, AbilitySetManager asm, ILogger<PhysxInitSystem> logger) : LarkSystem, ILarkSystemInit {
  public override Type[] RequiredComponents => [typeof(PhysxRigidbodyComponent)];

  private ILarkComponent[] GetCapsule(Vector3 pos, string name) => [
    // new MeshComponent("cube/cube.glb"),
    new MeshComponent("capsule/capsule.glb"),
    new MetadataComponent(name),
    new PhysxCapsuleComponent(.5f, 1f, false),
    // new PhysxBoxComponent(new(.95f, .95f, .95f), false),
    new PhysxRigidbodyComponent(1f, 0.01f, 0.01f, false),
    TransformComponent.Identity with { Position = pos + new Vector3(0, -10, 0), Rotation = LarkUtils.CreateFromYawPitchRoll(0, 0, 0)}
  ];

  private ILarkComponent[] GetCube(Vector3 pos, Vector3 scale, string name) => [
    new MeshComponent("cube/cube.glb"),
    // new MeshComponent("capsule/capsule.glb"),
    new MetadataComponent(name),
    // new PhysxCapsuleComponent(.5f, 1f, false),
    new PhysxBoxComponent(scale, true),
    // new PhysxRigidbodyComponent(1f, 0.01f, 0.01f, false),
    TransformComponent.Identity with { Position = pos, Scale = scale, Rotation = LarkUtils.CreateFromYawPitchRoll(0, 0, 0) }
  ];

  public Task Init() {
    // em.AddEntity(GetCapsule(new Vector3(0, 0, 0), "cube000"));
    // em.AddEntity(GetCapsule(new Vector3(2, 0, 0), "cube200"));
    // em.AddEntity(GetCapsule(new Vector3(4, 0, 0), "cube400"));
    // em.AddEntity(GetCapsule(new Vector3(6, 0, 0), "cube600"));
    // em.AddEntity(GetCapsule(new Vector3(-2, 0, 0), "cube-200"));
    // em.AddEntity(GetCapsule(new Vector3(-4, 0, 0), "cube-400"));
    // em.AddEntity(GetCapsule(new Vector3(-6, 0, 0), "cube-600"));

    // em.AddEntity(GetCapsule(new Vector3(0, 0, 2), "cube002"));
    // em.AddEntity(GetCapsule(new Vector3(0, 0, 4), "cube004"));
    // em.AddEntity(GetCapsule(new Vector3(0, 0, 6), "cube006"));
    // em.AddEntity(GetCapsule(new Vector3(0, 0, -2), "cube00-2"));
    // em.AddEntity(GetCapsule(new Vector3(0, 0, -4), "cube00-4"));
    // em.AddEntity(GetCapsule(new Vector3(0, 0, -6), "cube00-6"));

    // em.AddEntity(GetCapsule(new Vector3(0, 2, 0), "cube020"));
    // em.AddEntity(GetCapsule(new Vector3(0, -2, 0), "cube0-20"));

    em.AddEntity(GetCube(new Vector3(40, 0, 20), new Vector3(1, 10, 1), "cube000"));
    em.AddEntity(GetCube(new Vector3(50, 0, 30), new Vector3(100, 10, 3), "cube001"));

    return Task.CompletedTask;
  }

  public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
    // Log transform position
    // var (id, components) = Entity;
    // var transform = components.Get<TransformComponent>();
    // var entityRotation = Vector3.Transform(new Vector3(0, 0, -1), transform.Rotation);

    // // logger.LogInformation("Transform position: {pos}", transform.Position);
    // // logger.LogInformation("Transform position: {pos} :: {rot}", transform.Position, transform.Rotation);

    // // find camera entity.
    // var cameraEntityId = em.GetEntityIdsWithComponents(typeof(CameraComponent)).First();
    // var (cId, cComponents) = em.GetEntity(cameraEntityId);
    // var cameraTransform = cComponents.Get<TransformComponent>();

    // // Camera rotation vector. This is the direction the camera is facing.
    // var cameraRotation = Vector3.Transform(new Vector3(0, 0, -1), cameraTransform.Rotation);

    // // logger.LogInformation("Camera position: {pos} :: {rot}", cameraTransform.Position, cameraRotation);

    // // vector pointing to origin from transform position
    // var origin = new Vector3(0, 0, -10) + Vector3.Zero - transform.Position;

    // pm.AddForce(pm.GetActorId(id), new Vector3(1, 0, 1) * origin, PxForceMode.Force);
  }
}