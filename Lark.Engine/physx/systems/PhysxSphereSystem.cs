using System.Collections.Frozen;
using Lark.Engine.ecs;
using Lark.Engine.physx.components;
using Lark.Engine.physx.managers;
using Lark.Engine.std;

namespace Lark.Engine.physx.systems;

public record struct PhysxSphereComponent(float Radius, bool IsStatic = false) : ILarkComponent { }

public class PhysxSphereSystem(PhysxManager pm, EntityManager em, PhysxColliderManager pcm) : LarkSystem, ILarkSystemBeforeUpdate {
  public override Type[] RequiredComponents => [typeof(PhysxSphereComponent), typeof(TransformComponent)];

  public void BeforeUpdate() {
    foreach (var (id, components) in em.GetEntitiesWithComponentsSync(RequiredComponents)) {
      var transform = components.Get<TransformComponent>();
      var sphereComponent = components.Get<PhysxSphereComponent>();

      // If the entity doesn't have the LarkPhysxMarker component, add it.
      if (!components.Has<LarkPhysxMarker>()) {
        em.AddEntityComponent(id, new LarkPhysxMarker());
      }

      // If the actor has not been created yet, create it.
      if (!pm.HasActor(id)) {
        var actorId = pcm.RegisterSphere(transform.Position, transform.Rotation, sphereComponent.Radius, sphereComponent.IsStatic, id);
        pm.SetActorId(id, actorId);
      }
    }
  }
}