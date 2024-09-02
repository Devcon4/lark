
using System.Collections.Frozen;
using System.Numerics;
using Lark.Engine.ecs;
using Lark.Engine.std;
using Microsoft.Extensions.Logging;

namespace Lark.Engine.std.systems;
public class GlobalTransformSystem(SceneGraphManager sceneGraphManager, EntityManager em) : LarkSystem, ILarkSystemBeforeUpdate, ILarkSystemBeforeDraw {

  public override Type[] RequiredComponents => [typeof(TransformComponent)];

  public void BeforeDraw() {
    sceneGraphManager.UpdateGlobalTransforms();
  }

  public void BeforeUpdate() {
    // Check if any entity has a TransformComponent but not a GlobalTransformComponent, and add it
    foreach (var (entityId, components) in em.GetEntitiesWithComponentsSync(typeof(TransformComponent))) {
      if (!components.Has<GlobalTransformComponent>()) {
        em.AddEntityComponent(entityId, new GlobalTransformComponent());
      }
    }
  }
}