
using System.Collections.Frozen;
using Lark.Engine.ecs;
using Lark.Engine.std;

namespace Lark.Engine.std.systems;
public class GlobalTransformSystem(SceneGraphManager sceneGraphManager, EntityManager em) : LarkSystem, ILarkSystemBeforeUpdate, ILarkSystemAfterUpdate {

  public override Type[] RequiredComponents => [typeof(TransformComponent)];

  public void AfterUpdate() {
    // Calculate GlobalTransformComponent as the sum of all TransformComponent instances up the scene graph tree
    foreach (var (entityId, components) in em.GetEntitiesWithComponentsSync(typeof(GlobalTransformComponent))) {
      var globalTransform = components.Get<TransformComponent>();

      foreach (var ancestor in sceneGraphManager.GetBranch(entityId)) {
        var (_, ac) = em.GetEntity(ancestor);
        var transform = ac.Get<TransformComponent>();
        globalTransform = CombineTransforms(globalTransform, transform);
      }

      em.UpdateEntityComponent(entityId, new GlobalTransformComponent(globalTransform.Position, globalTransform.Scale, globalTransform.Rotation));
    }
  }

  public void BeforeUpdate() {
    // Check if any entity has a TransformComponent but not a GlobalTransformComponent, and add it
    foreach (var (entityId, components) in em.GetEntitiesWithComponentsSync(typeof(TransformComponent))) {
      if (!components.Has<GlobalTransformComponent>()) {
        em.AddEntityComponent(entityId, new GlobalTransformComponent());
      }
    }
  }

  public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
    // This system does not need to do anything in the Update method
  }

  private TransformComponent CombineTransforms(TransformComponent a, TransformComponent b) {
    // Combine two TransformComponent instances
    // This is a placeholder implementation and may need to be adjusted based on your specific requirements
    var position = a.Position + b.Position;
    var scale = a.Scale * b.Scale;
    var rotation = a.Rotation * b.Rotation;

    return new TransformComponent(position, scale, rotation);
  }
}