using System.Collections.Frozen;
using System.Numerics;
using Lark.Engine.ecs;
using Lark.Engine.physx.components;
using Lark.Engine.physx.managers;
using Lark.Engine.std;

namespace Lark.Engine.physx.systems;

public record struct PhysxCapsuleComponent(float Radius, float Height, bool IsStatic = false) : ILarkComponent { }

public class PhysxCapsuleSystem(PhysxManager pm, EntityManager em, PhysxColliderManager pcm) : LarkSystem, ILarkSystemBeforeUpdate {
  public override Type[] RequiredComponents => [typeof(PhysxCapsuleComponent)];

  public void BeforeUpdate() {
    foreach (var (id, components) in em.GetEntitiesWithComponentsSync(RequiredComponents)) {
      var transform = components.Get<TransformComponent>();
      var capsuleComponent = components.Get<PhysxCapsuleComponent>();

      // If the entity doesn't have the LarkPhysxMarker component, add it.
      if (!components.Has<LarkPhysxMarker>()) {
        em.AddEntityComponent(id, new LarkPhysxMarker());
      }

      // If the actor has not been created yet, create it.
      if (!pm.HasActor(id)) {

        // physx capsules are oriented along the y axis, so we need to rotate the transform component to match.
        var actorRotation = transform.Rotation * Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2);

        var actorId = pcm.RegisterCapsule(transform.Position, actorRotation, capsuleComponent.Radius, capsuleComponent.Height, capsuleComponent.IsStatic, id);
        pm.SetActorId(id, actorId);
      }
    }
  }
}