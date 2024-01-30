using System.Collections.Frozen;
using System.Numerics;
using Lark.Engine.ecs;
using Lark.Engine.std;
using Lark.Game.components;
using Microsoft.Extensions.Logging;

namespace Lark.Game.systems;

public record struct VelocityComponent(Vector3 MoveDelta, Vector3 JumpDelta) : ILarkComponent { }

public class JumpSystem(EntityManager em, TimeManager tm, ILogger<JumpSystem> logger) : LarkSystem {
  public override Type[] RequiredComponents => [typeof(VelocityComponent), typeof(JumpComponent)];



  public override void Update((Guid, FrozenSet<ILarkComponent>) entity) {
    var (key, components) = entity;
    var jump = components.Get<JumpComponent>();
    var transform = components.Get<TransformComponent>();
    var velocity = components.Get<VelocityComponent>();

    var End = jump.End;
    // var Progress = jump.Progress + (float)(tm.DeltaTime.TotalMilliseconds / jump.Duration.TotalMilliseconds);
    //2.1834
    var Progress = jump.Progress + (float)(2.1824f / jump.Duration.TotalMilliseconds);
    // var Progress = jump.Progress + (float)(jump.Duration.TotalMilliseconds / 12000);

    var JumpDelta = VectorUtils.Berp(Vector3.Zero, Vector3.Zero, Progress, CurveUtils.Jump);

    var newJump = jump with {
      End = End,
      JumpPosition = JumpDelta,
      VelocityDelta = jump.VelocityDelta + velocity.MoveDelta,
      Progress = Progress
    };

    em.UpdateEntityComponent(key, newJump);

    logger.LogInformation("Jump :: {progress} :: {position}", Progress, JumpDelta);

    // If the jump is complete, remove the JumpComponent and return
    if (jump.Progress >= 1) {
      JumpDelta = Vector3.Zero;
      logger.LogInformation("Jump :: Complete :: Final {finalPos} :: {key}", JumpDelta, key);
      em.RemoveEntityComponent<JumpComponent>(key);
    }

    var newVelocity = velocity with {
      JumpDelta = JumpDelta
    };

    em.UpdateEntityComponent(key, newVelocity);
  }
}