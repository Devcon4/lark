using JoltPhysicsSharp;
using Lark.Engine.ecs;
using Lark.Engine.jolt.components;
using Lark.Engine.jolt.managers;
using Lark.Engine.std;
using Microsoft.Extensions.Logging;

namespace Lark.Engine.jolt.systems;

public class JoltBodySystem(JoltManager jm, EntityManager em, ILogger<JoltBodySystem> logger) : LarkSystem, ILarkSystemBeforeUpdate {
  public override Type[] RequiredComponents => [typeof(TransformComponent), typeof(JoltBodyComponent)];

  public void BeforeUpdate() {
    foreach (var (id, components) in em.GetEntitiesWithComponentsSync(RequiredComponents)) {
      if (components.Has<JoltBodyInstanceComponent>()) {
        continue;
      }

      var settings = components.Get<JoltBodyComponent>();
      var transform = components.Get<TransformComponent>();

      var systemId = jm.SystemLookup[JoltManager.DefaultSystem];
      if (components.Has<JoltSystemComponent>()) {
        systemId = components.Get<JoltSystemComponent>().SystemId;
      }

      var creationSettings = new BodyCreationSettings(settings.Shape, transform.Position, transform.Rotation, settings.MotionType, settings.ObjectLayer);

      var bi = jm.GetBodyInterface(systemId);
      var bodyId = bi.CreateAndAddBody(creationSettings, Activation.Activate);

      logger.LogInformation("Created body {bodyID} for entity {key} with transform {transform}", bodyId, id, transform);

      em.AddEntityComponent(id, new JoltBodyInstanceComponent(systemId, bodyId));
    }
  }
}
