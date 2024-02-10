
using System.Collections.Frozen;
using System.Numerics;
using Lark.Engine.ecs;
using Lark.Engine.physx.components;
using Lark.Engine.physx.managers;
using Lark.Engine.std;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;

namespace Lark.Engine.physx.systems;

// System which registers PhysxPlaneComponents with the PhysxManager. If the entity is a regidbody update the transform component with the new position.
public record struct PhysxPlaneComponent() : ILarkComponent { }

public class PhysxPlaneSystem(EntityManager em, PhysxManager pm, PhysxColliderManager pcm, ILogger<PhysxPlaneSystem> logger) : LarkSystem {
  public override Type[] RequiredComponents => [typeof(PhysxPlaneComponent), typeof(TransformComponent)];

  public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
    var (id, components) = Entity;
    var transform = components.Get<TransformComponent>();

    // If the entity doesn't have the LarkPhysxMarker component, add it.
    if (!components.Has<LarkPhysxMarker>()) {
      em.AddEntityComponent(id, new LarkPhysxMarker());
    }

    // If the actor has not been created yet, create it.
    if (!pm.HasActor(id)) {
      // Calc up normal vector of the plane based on the rotation of the transform component.
      var up = -Vector3.UnitY;
      var normal = Vector3.Transform(up, transform.Rotation);
      logger.LogInformation("PhysxPlaneSystem :: pos {pos} :: normal {normal}", transform.Position, normal);

      var actorId = pcm.RegisterPlane(transform.Position, normal, id);
      pm.SetActorId(id, actorId);
    }
  }
}