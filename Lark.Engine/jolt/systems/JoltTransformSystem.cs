
using System.Collections.Frozen;
using Lark.Engine.ecs;
using Lark.Engine.jolt.components;
using Lark.Engine.jolt.managers;
using Lark.Engine.std;

namespace Lark.Engine.jolt.systems;

public class JoltTransformSystem(JoltManager jm, EntityManager em) : LarkSystem, ILarkSystemAfterUpdate {
  public override Type[] RequiredComponents => [typeof(TransformComponent), typeof(JoltBodyInstanceComponent)];

  public void AfterUpdate() {
    foreach (var (id, components) in em.GetEntitiesWithComponentsSync(RequiredComponents)) {
      var transform = components.Get<TransformComponent>();
      var (systemId, bodyId) = components.Get<JoltBodyInstanceComponent>();

      using var body = jm.GetBodyRead(systemId, bodyId);

      if (body is null) {
        return;
      }

      var pos = body.Instance.Position;
      var rot = body.Instance.Rotation;

      // Jolt is +y up, Lark is -y up, so we need to flip the y axis.
      pos.Y = -pos.Y;
      rot.Y = -rot.Y;

      var newTransform = transform with {
        Position = pos,
        Rotation = rot
      };

      em.UpdateEntityComponent(id, newTransform);
    }
  }
}
