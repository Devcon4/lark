using System.Collections.Frozen;
using System.Numerics;
using Lark.Engine.ecs;
using Lark.Engine.jolt.components;
using Lark.Engine.jolt.managers;
using Lark.Engine.std;
using Microsoft.Extensions.Logging;

namespace Lark.Engine.jolt.systems;

public class JoltCharacterTransformSystem(EntityManager em, JoltManager jm, ILogger<JoltCharacterTransformSystem> logger, SceneGraphManager sm) : LarkSystem, ILarkSystemAfterUpdate {
  public override Type[] RequiredComponents => [typeof(JoltCharacterInstance), typeof(TransformComponent)];

  public void AfterUpdate() {
    foreach (var (key, components) in em.GetEntitiesWithComponentsSync(RequiredComponents)) {
      var characterInstance = components.Get<JoltCharacterInstance>();
      var transform = components.Get<TransformComponent>();

      if (components.Has<JoltBodyInstanceComponent>() is true) {
        // Should not have both a JoltBodyComponent and a JoltCharacterInstance on the same entity. Recommend removing the JoltBodyComponent to a child entity.
        logger.LogWarning("JoltCharacterTransformSystem :: Should not have both a JoltBodyComponent and a JoltCharacterInstance on the same entity :: {key}", key);
      }

      var character = jm.GetCharacter(characterInstance.CharacterId);

      // Jolt is +y up, Lark is -y up, so we need to flip the y axis.
      var position = new Vector3(character.Position.X, character.Position.Y, character.Position.Z);
      var rotation = new Quaternion(character.Rotation.X, -character.Rotation.Y, character.Rotation.Z, -character.Rotation.W);

      var newTransform = transform with {
        Position = position,
        Rotation = rotation
      };

      logger.LogInformation("JoltCharacterTransformSystem :: Updating entity {key} with new transform {newTransform}", key, newTransform);
      em.UpdateEntityComponent(key, newTransform);
    }
  }
}