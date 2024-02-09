using System.Collections.Frozen;
using System.Numerics;
using Lark.Engine.ecs;
using Lark.Engine.physx.components;
using Lark.Engine.physx.managers;
using Lark.Engine.std;

namespace Lark.Engine.physx.systems;

public class PhysxTransformSystem(EntityManager em, PhysxManager pm) : LarkSystem {
  public override Type[] RequiredComponents => [typeof(TransformComponent)];

  public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
    var (id, components) = Entity;
    var transform = components.Get<TransformComponent>();

    if (!pm.HasActor(id)) {
      return;
    }

    var actorId = pm.GetActorId(id);

    var (pos, rot) = pm.GetActorTransform(actorId);

    // If the entity has a physxPlaneComponent, adjust rotation from physx to match engine space.
    if (components.Has<PhysxPlaneComponent>()) {
      rot = pm.GetPlaneRotation(actorId);
    }

    // If the entity has a physxCapsuleComponent, adjust rotation from physx to match engine space.
    if (components.Has<PhysxCapsuleComponent>()) {
      rot = pm.GetCapsuleRotation(actorId);
    }

    var newTransform = transform with {
      Position = pos,
      Rotation = rot
    };

    em.UpdateEntityComponent(id, newTransform);
  }

  public override async void AfterUpdate() {
    // cleanup actors which have been deleted

    var entities = em.GetEntityIdsWithComponents(typeof(LarkPhysxMarker));

    // delete actors which are not in the list
    foreach (var (id, actorId) in pm.EntityToActor) {
      if (!entities.Contains(id)) {
        pm.DeleteActor(actorId);
        continue;
      }

      // If transform was updated outside of the physx system, update the actor transform.
      var (pos, rot) = pm.GetActorTransform(actorId);
      var (_, components) = em.GetEntity(id);
      var transform = components.Get<TransformComponent>();
      if (transform.Position != pos || transform.Rotation != rot) {
        // TODO: Fix this, throws threading error.
        // pm.UpdateActorTransform(actorId, transform.Position, transform.Rotation);
      }
    }

    await Task.CompletedTask;
  }
}