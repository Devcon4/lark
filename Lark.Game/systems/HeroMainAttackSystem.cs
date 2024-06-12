
// using System.Collections.Frozen;
// using System.Numerics;
// using Lark.Engine.ecs;
// using Lark.Engine.physx.managers;
// using Lark.Engine.physx.systems;
// using Lark.Engine.std;
// using Microsoft.Extensions.Logging;

// namespace Lark.Game.systems;

// public class HeroMainAttackSystem(EntityManager em, PhysxManager pm, TimeManager tm, PhysxColliderManager pcm, ILogger<HeroMainAttackSystem> logger) : LarkSystem {
//   public override Type[] RequiredComponents => [typeof(TransformComponent), typeof(CastInstance<HeroMainAttack>)];

//   public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
//     logger.LogInformation("HeroMainAttackSystem :: Update :: {frame} :: {threadId}", tm.TotalFrames, System.Threading.Thread.CurrentThread.ManagedThreadId);
//     var (key, components) = Entity;
//     var (transform, attack) = components.Get<TransformComponent, CastInstance<HeroMainAttack>>();

//     var forward = Vector3.Transform(Vector3.UnitZ, transform.Rotation);

//     if (pcm.Raycast(out var hit, transform.Position, forward, attack.Ability.MaxDistance)) {
//       logger.LogInformation("HeroMainAttackSystem :: Hit entity {hitId}", hit);
//     }

//     em.RemoveEntity(key);
//   }
// }


// // if (pcm.Overlay(out var hits, actorId, transform.Position, transform.Rotation)) {
// //   logger.LogInformation("HeroMainAttackSystem :: Hit {count} objects", hits.Count);

// //   em.RemoveEntity(key);
// // }