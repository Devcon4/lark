using System.Collections.Frozen;
using System.Numerics;
using Lark.Engine.ecs;
using Lark.Engine.std;
using Microsoft.Extensions.Logging;

namespace Lark.Game.systems;

public record struct CharacterJumpComponent(ILarkCurve Curve, TimeSpan Duration, float Scale, Vector3 StartPosition, float Progress = 0) : ILarkComponent { }

public class CharacterJumpSystem(ILogger<CharacterJumpSystem> logger, EntityManager em, TimeManager tm) : LarkSystem {
  public override Type[] RequiredComponents => [typeof(CharacterJumpComponent), typeof(TransformComponent), typeof(CharacterDisplacementComponent)];

  public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
    var (key, components) = Entity;
    var jump = components.Get<CharacterJumpComponent>();
    // logger.LogInformation("Jump :: Update :: {key}", key);

    // If the jump is complete, remove the JumpComponent and return
    if (jump.Progress > 1) {
      em.RemoveEntityComponent<CharacterJumpComponent>(key);

      // clear jump delta
      var dis = components.Get<CharacterDisplacementComponent>();
      var clearDis = dis with { JumpDelta = Vector3.Zero };
      em.UpdateEntityComponent(key, clearDis);

      return;
    }

    var (transform, displacement) = components.Get<TransformComponent, CharacterDisplacementComponent>();

    var progress = jump.Progress + (float)(tm.DeltaTime.TotalMilliseconds / jump.Duration.TotalMilliseconds);
    var jumpAbs = VectorUtils.Berp(Vector3.Zero, Vector3.Zero, progress, jump.Curve);

    // jumpabs is the absolute position of the jump in world space, we need to turn it into a delta.
    var jumpDelta = jumpAbs - jump.StartPosition;

    var vec = Vector3.Transform(jumpDelta, transform.Rotation) * jump.Scale;
    var newDis = displacement with { JumpDelta = vec };
    var newJump = jump with { Progress = progress };

    em.UpdateEntityComponent(key, newDis);
    em.UpdateEntityComponent(key, newJump);
  }
}