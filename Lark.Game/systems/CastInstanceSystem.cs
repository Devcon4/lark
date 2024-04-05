
using System.Collections.Frozen;
using System.Numerics;
using Lark.Engine.ecs;
using Lark.Engine.std;
using Lark.Game.managers;
using Microsoft.Extensions.Logging;

namespace Lark.Game.systems;

public class CastInstanceSystem(EntityManager em, ILogger<CastInstanceSystem> logger) : LarkSystem {
  public override Type[] RequiredComponents => [typeof(TransformComponent), typeof(CastAbility<>)];

  public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
    var (key, components) = Entity;
    var transform = components.Get<TransformComponent>();
    var newTransform = transform with { Scale = Vector3.One };

    var castAbility = components.GetList<ICastAbility>();
    foreach (var ca in castAbility) {
      Cast(ca, newTransform, key);
    }
  }

  public void Cast(ICastAbility castAbility, TransformComponent newTransform, Guid key) {
    ILarkComponent castInstance = Activator.CreateInstance(
        typeof(CastInstance<>).MakeGenericType(castAbility.Ability.GetType()),
        [castAbility.Ability, castAbility.CreatedAt, castAbility.StartPosition, castAbility.Direction]
      ) as ILarkComponent
    ?? throw new InvalidOperationException("Failed to create CastInstance");

    var instanceId = em.AddEntity(newTransform, castInstance);
    logger.LogInformation("CastInstanceSystem :: Created instance {instanceId} of type {abilityType}", instanceId, castAbility.GetType().Name);

    if (castAbility.Ability is ICastPrefab prefabAbility) {
      logger.LogInformation("CastInstanceSystem :: Applying prefab ability {abilityType}", prefabAbility.GetType().Name);
      foreach (var c in prefabAbility.Components) {
        em.AddEntityComponent(instanceId, c);
      }
    }

    em.RemoveEntityComponent(key, c => c == castAbility);
  }
}


// if (pcm.Overlay(out var hits, actorId, transform.Position, transform.Rotation)) {
//   logger.LogInformation("HeroMainAttackSystem :: Hit {count} objects", hits.Count);

//   em.RemoveEntity(key);
// }