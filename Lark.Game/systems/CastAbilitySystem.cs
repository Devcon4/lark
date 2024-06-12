
// using System.Collections.Frozen;
// using System.Numerics;
// using Lark.Engine.ecs;
// using Lark.Engine.physx.systems;
// using Lark.Engine.std;
// using Lark.Game.managers;
// using NodaTime;

// namespace Lark.Game.systems;

// public interface IProjectileAbility : IBaseAbility, ICastPrefab {
//   public float ImpactDamage { get; }
//   public float HitboxRadius { get; }
//   public float Speed { get; }
//   public ILarkCurve FalloffCurve { get; }
//   public ILarkCurve DropCurve { get; }

//   public Duration MaxDuration { get; }
// }

// public interface ICooldownAbility : IBaseAbility {
//   public Duration Cooldown { get; }
// }

// public interface ICastPrefab : IBaseAbility {
//   public IEnumerable<ILarkComponent> Components { get; }
// }


// public interface IHitscanAbility : IBaseAbility {
//   public float ImpactDamage { get; }
//   public float HitboxRadius { get; }
//   public float Speed { get; }
//   public float MaxDistance { get; }
//   public ILarkCurve FalloffCurve { get; }
// }
// public record struct HeroMainAttack(
//   float ImpactDamage,
//   float HitboxRadius,
//   float Speed,
//   float MaxDistance,
//   Duration Cooldown,
//   ILarkCurve FalloffCurve
// ) : IHitscanAbility, ICooldownAbility, IBaseAbility { }

// public record struct HeroAltAttack(
//   float ImpactDamage,
//   float HitboxRadius,
//   float Speed,
//   Duration Cooldown,
//   Duration MaxDuration,
//   ILarkCurve FalloffCurve,
//   ILarkCurve DropCurve
// ) : IProjectileAbility, ICooldownAbility, ICastPrefab, IBaseAbility {
//   public readonly IEnumerable<ILarkComponent> Components => [
//     new PhysxSphereComponent(HitboxRadius),
//     new PhysxRigidbodyComponent(10f, IsKinematic: true),
//     new MeshComponent("cube/cube.glb"),
//     new MetadataComponent("HeroAltAttack::Instance")
//   ];
// }

// public record struct ActiveAbilitySet(string SetName) : ILarkComponent { };
// public record struct TriggerAbility(string AbilityName) : IBaseAbility { };

// public record struct AbilityCastResult(Instant Time, string AbilityName) : ILarkComponent { };

// public interface ICastAbility : ILarkComponent {
//   public IBaseAbility Ability { get; }
//   public Instant CreatedAt { get; }
//   public Vector3 StartPosition { get; }
//   public Quaternion Direction { get; }
// }

// public record struct CastAbility<T>(T Ability, Instant CreatedAt, Vector3 StartPosition, Quaternion Direction) : ICastAbility where T : IBaseAbility {
//   readonly IBaseAbility ICastAbility.Ability => Ability;
// };

// public record struct CastInstance<T>(T Ability, Instant CreatedAt, Vector3 StartPosition, Quaternion Direction) : ILarkComponent where T : IBaseAbility { }

// public class CastAbilitySystem(EntityManager em, AbilitySetManager am, TimeManager tm) : LarkSystem {
//   public override Type[] RequiredComponents => [typeof(ActiveAbilitySet), typeof(TriggerAbility)];

//   // public override Task Init() {
//   //   // am.CreateAbilitySet("Hero1");

//   //   // am.AddAbilityToSet("Hero1", "MainAttack", new HeroMainAttack(85f, .5f, 1f, CurveUtils.Linear, CurveUtils.Linear));
//   //   // am.AddAbilityToSet("Hero1", "AltAttack", new HeroAltAttack(85f, .5f, 1f, TimeSpan.FromSeconds(3), CurveUtils.Linear, CurveUtils.Linear));

//   //   return Task.CompletedTask;
//   // }

//   public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
//     var (key, components) = Entity;
//     var activeSet = components.Get<ActiveAbilitySet>();
//     var triggers = components.GetList<TriggerAbility>();
//     var transform = components.Get<TransformComponent>();

//     foreach (var trigger in triggers) {
//       IBaseAbility ability = am.GetAbility(activeSet.SetName, trigger.AbilityName);

//       switch (ability) {
//         case ICooldownAbility cooldownAbility when
//           components.GetList<AbilityCastResult>().FirstOrDefault(c => c.AbilityName == trigger.AbilityName) is AbilityCastResult lastCast
//             && tm.Now - lastCast.Time <= cooldownAbility.Cooldown:

//           // Build castAbility generic using the type of the ability.
//           CastAbility(key, ability, transform, trigger);
//           break;
//         default:
//           CastAbility(key, ability, transform, trigger);
//           break;
//       }
//       em.RemoveEntityComponent(key, t => t is TriggerAbility ta && ta.AbilityName == trigger.AbilityName);
//     }
//     void CastAbility(Guid key, IBaseAbility ability, TransformComponent transform, TriggerAbility trigger) {
//       var createdTime = tm.Now;
//       ILarkComponent castAbility = Activator.CreateInstance(
//             typeof(CastAbility<>).MakeGenericType(ability.GetType()),
//             [ability, createdTime, transform.Position, transform.Rotation]
//           ) as ILarkComponent
//         ?? throw new InvalidOperationException("Failed to create CastAbility");

//       em.AddEntityComponent(key, castAbility);
//       em.AddEntityComponent(key, new AbilityCastResult(createdTime, trigger.AbilityName));
//     }
//   }
// }