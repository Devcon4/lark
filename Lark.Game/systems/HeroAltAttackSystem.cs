
using System.Collections.Frozen;
using System.Numerics;
using Lark.Engine.ecs;
using Lark.Engine.physx.managers;
using Lark.Engine.std;
using Microsoft.Extensions.Logging;

namespace Lark.Game.systems;

public class HeroAltAttackSystem(EntityManager em, PhysxManager pm, TimeManager tm, PhysxColliderManager pcm, ILogger<HeroAltAttackSystem> logger) : LarkSystem {
  public override Type[] RequiredComponents => [typeof(TransformComponent), typeof(CastInstance<HeroAltAttack>)];

  public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
    logger.LogInformation("HeroAltAttackSystem :: Update :: {frame} :: {threadId}", tm.TotalFrames, System.Threading.Thread.CurrentThread.ManagedThreadId);
    var (key, components) = Entity;
    var (transform, attack) = components.Get<TransformComponent, CastInstance<HeroAltAttack>>();

    // now at the start of the frame
    var frameNow = tm.Now;
    var finalTime = attack.CreatedAt + attack.Ability.MaxDuration;

    if (pm.HasActor(key) is false) {
      return;
    }

    var actorId = pm.GetActorId(key);
    if (frameNow > finalTime) {
      logger.LogInformation("HeroAltAttackSystem :: Ability expired");
      em.RemoveEntity(key);
      return;
    }

    if (pcm.Overlay(out var hits, actorId, transform.Position, transform.Rotation)) {
      if (hits.Count > 0) {
        logger.LogInformation("HeroAltAttackSystem :: Hit {count} objects", hits.Count);
        em.RemoveEntity(key);
        return;
      }
    }

    var forward = Vector3.UnitZ * (float)(attack.Ability.Speed * attack.Ability.MaxDuration.TotalSeconds);
    var rotForward = Vector3.Transform(forward, Quaternion.Normalize(attack.Direction));
    var finalPosition = attack.StartPosition + rotForward;
    var t = 1 - (float)((finalTime - frameNow) / attack.Ability.MaxDuration);

    // Rotation
    logger.LogInformation("HeroAltAttackSystem :: Firing in direction {rotation}", attack.Direction);

    // moving from start to end position
    // logger.LogInformation("HeroAltAttackSystem :: Moving from {start} to {end} at {percent}%", attack.StartPosition, finalPosition, t * 100);

    // Move the attack along its curve
    var position = VectorUtils.Berp(attack.StartPosition, finalPosition, t, attack.Ability.DropCurve);
    logger.LogInformation("HeroAltAttackSystem :: Moving from {start} to {end} at {percent}% and {position}", attack.StartPosition, finalPosition, t * 100, position);

    pm.Move(actorId, position);
  }
}

// if (pcm.Overlay(out var hits, actorId, transform.Position, transform.Rotation)) {
//   logger.LogInformation("HeroMainAttackSystem :: Hit {count} objects", hits.Count);

//   em.RemoveEntity(key);
// }