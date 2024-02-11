using System.Collections.Frozen;
using Lark.Engine.ecs;
using Lark.Engine.physx;
using Lark.Engine.physx.managers;
using Lark.Engine.std;

namespace Lark.Game.systems;

public class CharacterTransformSystem(PhysxCharacterManager pcm, EntityManager em) : LarkSystem {
  public override Type[] RequiredComponents => [typeof(PhysxCharacterComponent), typeof(CharacterDisplacementComponent), typeof(TransformComponent)];

  public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
    var (key, components) = Entity;
    if (!pcm.HasController(key)) {
      return;
    }

    var transform = components.Get<TransformComponent>();
    var position = pcm.GetPosition(key);
    em.UpdateEntityComponent(key, transform with { Position = position });
  }
}