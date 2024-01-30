using System.Collections.Frozen;
using Lark.Engine.ecs;
using Lark.Engine.physx.managers;
using Microsoft.Extensions.Logging;

namespace Lark.Engine.physx.systems;

public record struct PhysxRigidbodyComponent(float Mass, float LinearDamping = 0f, float AngularDamping = 0f, bool IsKinematic = false) : ILarkComponent { }

public class PhysxRigidbodySystem(PhysxManager pm, ILogger<PhysxRigidbodySystem> logger) : LarkSystem {
  public override Type[] RequiredComponents => [typeof(PhysxRigidbodyComponent)];

  public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
    // Update the rigidbody component.
    var (id, components) = Entity;
    var rigidbodyComponent = components.Get<PhysxRigidbodyComponent>();

    if (!pm.HasActor(id)) {
      logger.LogWarning("Entity {EntityId} has a rigidbody component but no actor.", id);
      return;
    }

    if (components.TryGet<PhysxPlaneComponent>(out _)) {
      logger.LogWarning("Entity {EntityId} has a rigidbody component but also a physxPlaneComponent. Only a dynamic actor can have a rigidbody.", id);
      return;
    }

    var isStatic = components.Any(c => c is PhysxBoxComponent box && box.IsStatic || c is PhysxCapsuleComponent capsule && capsule.IsStatic || c is PhysxSphereComponent sphere && sphere.IsStatic);
    if (isStatic) {
      logger.LogWarning("Entity {EntityId} has a static component but also a rigidbody component. Only a dynamic actor can have a rigidbody.", id);
      return;
    }

    // Get existing rigidbody. If it is different, update it.
    var actorId = pm.GetActorId(id);
    // var (mass, linearDamping, angularDamping, isKinematic) = pm.GetRigidbody(actorId);
    // if (mass != rigidbodyComponent.Mass || linearDamping != rigidbodyComponent.LinearDamping || angularDamping != rigidbodyComponent.AngularDamping || isKinematic != rigidbodyComponent.IsKinematic) {
    // }
    pm.UpdateRigidbody(actorId, rigidbodyComponent.Mass, rigidbodyComponent.LinearDamping, rigidbodyComponent.AngularDamping, rigidbodyComponent.IsKinematic);
  }
}