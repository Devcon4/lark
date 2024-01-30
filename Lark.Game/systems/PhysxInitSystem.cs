using System.Collections.Frozen;
using System.Numerics;
using Lark.Engine.ecs;
using Lark.Engine.physx.managers;
using Lark.Engine.physx.systems;
using Lark.Engine.std;
using MagicPhysX;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Lark.Game.systems;

// Testing system which creates a plane and a cube and drops the cube onto the plane. The cube should use the antique camera mesh.
public class PhysxInitSystem(EntityManager em, PhysxManager pm, ILogger<PhysxInitSystem> logger) : LarkSystem {
  public override Type[] RequiredComponents => [typeof(PhysxRigidbodyComponent)];

  private ILarkComponent[] GetCube(Vector3 pos, string name) => [
    new MeshComponent("cube/cube.glb"),
    new MetadataComponent(name),
    new PhysxBoxComponent(new(.95f, .95f, .95f), false),
    new PhysxRigidbodyComponent(1f, 0.01f, 0.01f, false),
    TransformComponent.Identity with { Position = pos + new Vector3(0, -10, 0)}
  ];

  public override Task Init() {
    em.AddEntity(GetCube(new Vector3(0, 0, 0), "cube000"));
    em.AddEntity(GetCube(new Vector3(2, 0, 0), "cube200"));
    em.AddEntity(GetCube(new Vector3(4, 0, 0), "cube400"));
    em.AddEntity(GetCube(new Vector3(6, 0, 0), "cube600"));
    em.AddEntity(GetCube(new Vector3(-2, 0, 0), "cube-200"));
    em.AddEntity(GetCube(new Vector3(-4, 0, 0), "cube-400"));
    em.AddEntity(GetCube(new Vector3(-6, 0, 0), "cube-600"));

    em.AddEntity(GetCube(new Vector3(0, 0, 2), "cube002"));
    em.AddEntity(GetCube(new Vector3(0, 0, 4), "cube004"));
    em.AddEntity(GetCube(new Vector3(0, 0, 6), "cube006"));
    em.AddEntity(GetCube(new Vector3(0, 0, -2), "cube00-2"));
    em.AddEntity(GetCube(new Vector3(0, 0, -4), "cube00-4"));
    em.AddEntity(GetCube(new Vector3(0, 0, -6), "cube00-6"));

    em.AddEntity(GetCube(new Vector3(0, 2, 0), "cube020"));
    em.AddEntity(GetCube(new Vector3(0, -2, 0), "cube0-20"));

    return Task.CompletedTask;
  }

  public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
    // Log transform position
    var (id, components) = Entity;
    var transform = components.Get<TransformComponent>();
    var entityRotation = Vector3.Transform(new Vector3(0, 0, -1), transform.Rotation);

    // logger.LogInformation("Transform position: {pos}", transform.Position);
    // logger.LogInformation("Transform position: {pos} :: {rot}", transform.Position, transform.Rotation);

    // find camera entity.
    var cameraEntityId = em.GetEntityIdsWithComponents(typeof(CameraComponent)).First();
    var (cId, cComponents) = em.GetEntity(cameraEntityId);
    var cameraTransform = cComponents.Get<TransformComponent>();

    // Camera rotation vector. This is the direction the camera is facing.
    var cameraRotation = Vector3.Transform(new Vector3(0, 0, -1), cameraTransform.Rotation);

    // logger.LogInformation("Camera position: {pos} :: {rot}", cameraTransform.Position, cameraRotation);

    // vector pointing to origin from transform position
    var origin = new Vector3(0, 0, -10) + Vector3.Zero - transform.Position;

    // pm.AddForce(pm.GetActorId(id), new Vector3(1, 0, 1) * origin, PxForceMode.Force);
  }
}