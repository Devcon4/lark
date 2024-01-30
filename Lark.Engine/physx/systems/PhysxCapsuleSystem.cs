using System.Collections.Frozen;
using Lark.Engine.ecs;
using Lark.Engine.physx.components;
using Lark.Engine.physx.managers;
using Lark.Engine.std;

namespace Lark.Engine.physx.systems;

public record struct PhysxCapsuleComponent(float Radius, float Height, bool IsStatic = false) : ILarkComponent { }

public class PhysxCapsuleSystem(PhysxManager pm, EntityManager em) : LarkSystem {
  public override Type[] RequiredComponents => [typeof(PhysxCapsuleComponent)];

  public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
    // Update the capsule component.
    var (id, components) = Entity;
    var transform = components.Get<TransformComponent>();
    var capsuleComponent = components.Get<PhysxCapsuleComponent>();

    // If the entity doesn't have the LarkPhysxMarker component, add it.
    if (!components.Has<LarkPhysxMarker>()) {
      em.AddEntityComponent(id, new LarkPhysxMarker());
    }

    // If the actor has not been created yet, create it.
    if (!pm.HasActor(id)) {
      var actorId = pm.RegisterCapsule(transform.Position, transform.Rotation, capsuleComponent.Radius, capsuleComponent.Height, capsuleComponent.IsStatic, id);
      pm.SetActorId(id, actorId);
    }
  }
}