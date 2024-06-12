using System.Collections.Frozen;
using JoltPhysicsSharp;
using Lark.Engine.ecs;
using Lark.Engine.jolt.components;
using Lark.Engine.jolt.managers;
using Lark.Engine.std.components;
using Lark.Engine.std.systems;
using Microsoft.Extensions.Logging;

namespace Lark.Engine.jolt.systems;

public class JoltConstraintSystem(EntityManager em, JoltManager jm, ILogger<JoltConstraintSystem> logger) : LarkSystem, ILarkSystemBeforeUpdate {
  public const string MissingConstraintError = "MISSING_CONSTRAINT_ERROR";
  public override Type[] RequiredComponents => [typeof(JoltConstraintComponent), typeof(JoltBodyInstanceComponent)];

  public void BeforeUpdate() {
    foreach (var entity in em.GetEntitiesWithComponentsSync([typeof(LarkSceneGraphComponent), typeof(JoltBodyInstanceComponent)])) {
      var (key, components) = entity;

      var hasIgnoreWarning = components.TryGet<LarkIgnoreWarningComponent>(out var warningComponent) && warningComponent.Code == MissingConstraintError;
      if (components.Has<JoltConstraintComponent>() is false && hasIgnoreWarning is false) {
        // This entity is apart of the scene graph and has a JoltBodyComponent, but no JoltConstraintComponent. This is usually a mistake and could cause the jolt system to be out of sync from the world.
        logger.LogError("JoltConstraintSystem :: Entity is apart of the scene graph and has a JoltBodyComponent, but no JoltConstraintComponent :: {key}", key);
      }
    }
  }

  public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
    var (key, components) = Entity;
    var constraint = components.Get<JoltConstraintComponent>();
    var body = components.Get<JoltBodyInstanceComponent>();
    constraint.SystemId ??= jm.GetSystemId(JoltManager.DefaultSystem);

    var (parentKey, parentComponents) = em.GetEntity(constraint.ParentEntityId);

    if (parentComponents.Has<JoltBodyComponent>()) {
      // If the parent entity has a JoltBodyComponent, most likely it hasen't been instanciaited yet. We will try again next frame.
      return;
    }

    if (parentComponents.Has<JoltBodyInstanceComponent>() is false) {
      logger.LogError("JoltConstraintSystem :: Parent entity does not have a JoltBodyComponent :: {key} -> {parentKey}", key, parentKey);
      return;
    }

    if (body.SystemId != constraint.SystemId) {
      logger.LogError("JoltConstraintSystem :: Constraints must be in the same system :: {key} -> {parentKey}", key, parentKey);
      return;
    }

    var parentBody = parentComponents.Get<JoltBodyInstanceComponent>();
    var constraintId = jm.AddConstraint(parentBody.BodyId, body.BodyId, constraint.Settings, constraint.SystemId);

    var joltConstraintInstance = new JoltConstraintInstance(constraintId);

    em.RemoveEntityComponent<JoltConstraintComponent>(key);
    em.AddEntityComponent(key, joltConstraintInstance);
  }
}
