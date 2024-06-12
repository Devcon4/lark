using System.Numerics;
using Lark.Engine.ecs;
using Lark.Engine.std;
using Microsoft.Extensions.Logging;

namespace Lark.Game.managers;

public record struct AbilitySetEntityMarker : ILarkComponent { }
public record struct AbilitySetComponent(string SetName, Dictionary<string, IBaseAbility> Abilities) : ILarkComponent;
public interface IBaseAbility : ILarkComponent { }

public class AbilitySetManager(ILogger<AbilitySetManager> logger, EntityManager em) : LarkManager {

  public static Type[] AbilitySetEntity => [typeof(AbilitySetEntityMarker)];

  public override Task Init() {
    em.AddEntity(new MetadataComponent("AbilitySet"), new AbilitySetEntityMarker());
    return Task.CompletedTask;
  }

  // GetAbility
  public IBaseAbility GetAbility(string setName, string abilityName) {
    var (id, components) = em.GetEntity(AbilitySetEntity);
    AbilitySetComponent? set = components.GetList<AbilitySetComponent>().FirstOrDefault(c => c.SetName == setName);

    if (set is null) {
      throw new Exception($"Ability set {setName} does not exist");
    }

    if (set?.Abilities.TryGetValue(abilityName, out IBaseAbility? ability) is null or false) {
      throw new Exception($"Ability {abilityName} does not exist in set {setName}");
    }

    return ability;
  }

  // CreateAbilitySet. A hero might have multiple abilities. Their types are defined in the AbilitySetEntityMarker.
  // We can use this to trigger abilities.
  public void CreateAbilitySet(string setName) {
    logger.LogInformation("Creating ability set {setName}", setName);
    var (id, _) = em.GetEntity(AbilitySetEntity);
    em.AddEntityComponent(id, new AbilitySetComponent(setName, []));
  }

  // RemoveAbilitySet
  public void RemoveAbilitySet(string setName) {
    var (id, components) = em.GetEntity(AbilitySetEntity);
    em.RemoveEntityComponent(id, c => c is AbilitySetComponent set && set.SetName == setName);
  }

  // RenameAbilitySet
  public void RenameAbilitySet(string oldName, string newName) {
    var (id, components) = em.GetEntity(AbilitySetEntity);

    AbilitySetComponent? existing = components.GetList<AbilitySetComponent>().FirstOrDefault(c => c.SetName == newName);
    if (existing is null) {
      throw new Exception($"Ability set {newName} already exists");
    }

    AbilitySetComponent? old = components.GetList<AbilitySetComponent>().FirstOrDefault(c => c.SetName == oldName);
    if (old is null) {
      throw new Exception($"Ability set {oldName} does not exist");
    }

    em.UpdateEntityComponent(id, (AbilitySetComponent)old with {
      SetName = newName
    });
  }

  // AddAbilityToSet
  public void AddAbilityToSet(string setName, string abilityName, IBaseAbility ability) {
    logger.LogInformation("Adding ability {abilityName} to set {setName}", abilityName, setName);
    var (key, components) = em.GetEntity(AbilitySetEntity);
    var set = components.Get<AbilitySetComponent>();

    if (set.Abilities.ContainsKey(abilityName)) {
      throw new Exception($"Ability {abilityName} already exists in set {setName}");
    }

    set.Abilities.Add(abilityName, ability);
    em.UpdateEntityComponent(key, set);
  }

  // RemoveAbilityFromSet
  public void RemoveAbilityFromSet(string setName, string abilityName) {
    var (key, components) = em.GetEntity(AbilitySetEntity);
    var set = components.Get<AbilitySetComponent>();

    if (!set.Abilities.ContainsKey(abilityName)) {
      throw new Exception($"Ability {abilityName} does not exist in set {setName}");
    }

    set.Abilities.Remove(abilityName);
    em.UpdateEntityComponent(key, set);
  }
}
