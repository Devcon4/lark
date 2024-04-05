using System.Collections.Frozen;
using System.Numerics;
using Lark.Engine.ecs;
using Lark.Engine.physx;
using Lark.Engine.physx.managers;
using Lark.Engine.std;

namespace Lark.Game.systems;

public class CharacterTransformSystem(PhysxCharacterManager pcm, EntityManager em) : LarkSystem {
  public override Type[] RequiredComponents => [typeof(CharacterRotationComponent), typeof(CharacterDisplacementComponent), typeof(CharacterComponent)];

  public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
    var (key, _) = Entity;

    // Get fresh components, as they may have been updated by other systems in the same frame
    var (_, components) = em.GetEntity(key);

    var rotDisplacement = components.Get<CharacterRotationComponent>();
    var charBase = components.Get<CharacterComponent>();

    var (_, targetComponents) = em.GetEntity(charBase.TargetId);
    var targetTransform = targetComponents.Get<TransformComponent>();
    var controllerTransform = components.Get<TransformComponent>();

    if (!pcm.HasController(charBase.TargetId)) {
      return;
    }

    var position = pcm.GetPosition(charBase.TargetId);

    em.UpdateEntityComponent(key, controllerTransform with { Rotation = rotDisplacement.Rotation });
    em.UpdateEntityComponent(charBase.TargetId, targetTransform with { Position = position });
  }
}