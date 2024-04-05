using System.Collections.Frozen;
using System.Numerics;
using Lark.Engine.ecs;
using Lark.Engine.physx.components;
using Lark.Engine.physx.managers;
using Lark.Engine.std;

namespace Lark.Engine.physx.systems;

public record struct PhysxBoxComponent(Vector3 Scale, bool IsStatic = false) : ILarkComponent { }

public class PhysxBoxSystem(EntityManager em, PhysxColliderManager pcm, PhysxManager pm) : LarkSystem, ILarkSystemBeforeUpdate {
  public override Type[] RequiredComponents => [typeof(PhysxBoxComponent), typeof(TransformComponent)];

  public void BeforeUpdate() {
    foreach (var (id, components) in em.GetEntitiesWithComponentsSync(RequiredComponents)) {
      var transform = components.Get<TransformComponent>();
      var boxComponent = components.Get<PhysxBoxComponent>();

      // If the entity doesn't have the LarkPhysxMarker component, add it.
      if (!components.Has<LarkPhysxMarker>()) {
        em.AddEntityComponent(id, new LarkPhysxMarker());
      }

      // If the actor has not been created yet, create it.
      if (!pm.HasActor(id)) {
        var actorId = pcm.RegisterBox(transform.Position, transform.Rotation, boxComponent.Scale, boxComponent.IsStatic, id);
        pm.SetActorId(id, actorId);
      }
    }
  }
}