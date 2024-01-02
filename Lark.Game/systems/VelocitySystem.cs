using System.Collections.Frozen;
using System.Numerics;
using Lark.Engine.ecs;
using Lark.Engine.std;
using Microsoft.Extensions.Logging;

namespace Lark.Game.systems;

public class VelocitySystem(EntityManager em, ILogger<VelocitySystem> logger) : LarkSystem {
  public override Type[] RequiredComponents => [typeof(VelocityComponent), typeof(TransformComponent)];

  public override Task Init() {
    return Task.CompletedTask;
  }

  public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
    var (key, components) = Entity;
    var (velocity, transform) = components.Get<VelocityComponent, TransformComponent>();

    var newTransform = transform with {
      Position = transform.Position + velocity.JumpDelta + velocity.MoveDelta
    };

    em.UpdateEntityComponent(key, newTransform);
  }

  public override async void AfterUpdate() {
    // Clear the velocity of all entities
    await foreach (var (key, components) in em.GetEntitiesWithComponents([typeof(VelocityComponent)])) {
      em.UpdateEntityComponent(key, new VelocityComponent());
    }
  }
}