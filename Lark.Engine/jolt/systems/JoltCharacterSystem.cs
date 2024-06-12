using System.Collections.Frozen;
using System.Numerics;
using Lark.Engine.ecs;
using Lark.Engine.jolt.components;
using Lark.Engine.jolt.managers;
using Lark.Engine.std;
using Microsoft.Extensions.Logging;

namespace Lark.Engine.jolt.systems;

public class JoltCharacterSystem(ILogger<JoltCharacterSystem> logger, JoltManager jm, EntityManager em) : LarkSystem {
  public override Type[] RequiredComponents => [typeof(JoltCharacterComponent), typeof(TransformComponent)];

  public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
    var (key, components) = Entity;
    var character = components.Get<JoltCharacterComponent>();
    var transform = components.Get<TransformComponent>();

    // We asume all characters are in the default system
    var systemId = jm.GetSystemId(JoltManager.DefaultSystem);

    var position = transform.Position;
    var rotation = transform.Rotation;

    var createInfo = new CharacterCreateInfo(systemId, character.Shape, position, rotation, character.MaxSlopeAngle, character.Mass);
    var characterId = jm.CreateCharacter(createInfo);

    logger.LogInformation("Created character {characterId} for entity {key}", characterId, key);

    var joltCharacterInstance = new JoltCharacterInstance(characterId, systemId);

    em.RemoveEntityComponent<JoltCharacterComponent>(key);
    em.AddEntityComponent(key, joltCharacterInstance);
  }
}
