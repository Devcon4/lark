
using System.Collections.Frozen;
using Lark.Engine.ecs;
using Microsoft.Extensions.Logging;

namespace Lark.Engine.std.systems;

public record struct LarkSceneGraphComponent(Guid Parent, bool HasUpdated = false) : ILarkComponent { }

public class SceneGraphSystem(SceneGraphManager sgm, ILogger<SceneGraphSystem> logger) : LarkSystem {
  public override Type[] RequiredComponents => [typeof(LarkSceneGraphComponent)];

  public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
    var (entity, components) = Entity;
    var sceneGraph = components.Get<LarkSceneGraphComponent>();

    // If hasUpdated is true but doesn't exist in the graph, log a warning
    if (sceneGraph.HasUpdated && !sgm.HasNode(entity)) {
      logger.LogWarning("SceneGraph :: Entity {Entity} has updated but doesn't exist in the graph", entity);
    }

    if (sceneGraph.HasUpdated) {
      sgm.UpdateNode(entity, sceneGraph.Parent);

      return;
    }

    if (!sgm.HasNode(entity)) {
      sgm.AddNode(sceneGraph.Parent, entity);
    }
  }
}