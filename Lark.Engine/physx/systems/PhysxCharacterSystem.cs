using System.Collections.Frozen;
using Lark.Engine.ecs;
using Lark.Engine.physx.managers;
using Lark.Engine.std;
using Microsoft.Extensions.Logging;

namespace Lark.Engine.physx.systems;

public class PhysxCharacterSystem(PhysxCharacterManager pcm, EntityManager em) : LarkSystem {
  public override Type[] RequiredComponents => [typeof(PhysxCharacterComponent), typeof(TransformComponent)];

  public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
    var (entityId, components) = Entity;
    var (characterComponent, transform) = components.Get<PhysxCharacterComponent, TransformComponent>();

    // Create
    if (!pcm.HasController(entityId)) {

      var newId = pcm.CreateController(entityId, characterComponent.Radius, characterComponent.Height, transform.Position);

      var updated = characterComponent with { ControllerId = newId };
      em.UpdateEntityComponent(entityId, updated);
      return;
    }

    var controllerId = characterComponent.ControllerId!.Value;

  }
}
