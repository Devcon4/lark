using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;

namespace Lark.Engine.ecs;

public class EntityManager(ILogger<EntityManager> logger) : LarkManager {

  private ConcurrentDictionary<Guid, FrozenSet<ILarkComponent>> entities = new();
  private ConcurrentDictionary<Guid, FrozenSet<Type>> entityComponents = new();

  private ConcurrentDictionary<Type, HashSet<Guid>> entitiesByComponentType = new();

  public Guid AddEntity(params ILarkComponent[] components) {
    Guid key = Guid.NewGuid();
    return AddEntity(key, components);
  }

  public Guid AddEntity(Guid key, params ILarkComponent[] components) {

    logger.LogDebug("Adding entity {key}", key);

    if (entities.ContainsKey(key)) {
      throw new Exception($"Entity {key} already exists");
    }

    var componentTypes = components.Select(c => c.GetType()).ToFrozenSet();
    // if (componentTypes.Count != components.Length) {
    //   throw new Exception($"Components must be unique on entity: {key}");
    // }

    entities.TryAdd(key, components.ToFrozenSet());
    entityComponents.TryAdd(key, componentTypes);
    foreach (var component in components) {
      var componentType = component.GetType();

      if (!entitiesByComponentType.TryGetValue(componentType, out HashSet<Guid>? value)) {
        value = new HashSet<Guid>();
        entitiesByComponentType.TryAdd(componentType, value);
      }

      value.Add(key);
    }

    return key;
  }

  // GetTotalNumberOfEntities 
  public int GetEntitiesCount() {
    return entities.Count;
  }

  public ReadOnlySpan<ValueTuple<Guid, FrozenSet<ILarkComponent>, FrozenSet<Type>>> GetEntities() {
    var entitiesList = new ValueTuple<Guid, FrozenSet<ILarkComponent>, FrozenSet<Type>>[entities.Count];
    int i = 0;
    foreach (var entity in entities) {
      entitiesList[i] = new(entity.Key, entity.Value, entityComponents[entity.Key]);
      i++;
    }

    return entitiesList;
  }

  // GetEntity(type[] componentTypes): Find the first entity that has all of the specified components.
  public ValueTuple<Guid, FrozenSet<ILarkComponent>> GetEntity(params Type[] componentTypes) {
    var componentTypeSet = new HashSet<Type>(componentTypes);

    foreach (var entity in entities) {
      if (entityComponents[entity.Key].IsSupersetOf(componentTypeSet)) {
        return new(entity.Key, entity.Value);
      }
    }

    throw new Exception("No entity found with specified components");
  }

  public ValueTuple<Guid, FrozenSet<ILarkComponent>> GetEntity(Guid id) {
    if (!entities.TryGetValue(id, out var value)) {
      throw new Exception("Entity does not exist");
    }

    return new(id, value);
  }

  // public FrozenSet<ValueTuple<Guid, FrozenSet<ILarkComponent>>> GetEntitiesWithComponents(params Type[] componentTypes) {
  //   var entitiesWithComponents = new HashSet<ValueTuple<Guid, FrozenSet<ILarkComponent>>>();
  //   var componentTypeSet = new HashSet<Type>(componentTypes);

  //   foreach (var entity in entities) {
  //     var components = entity.Value.ToFrozenSet();
  //     if (entityComponents[entity.Key].IsSupersetOf(componentTypeSet)) {
  //       entitiesWithComponents.Add(new(entity.Key, components));
  //     }
  //   }

  //   return entitiesWithComponents.ToFrozenSet();
  // }

  public IEnumerable<ValueTuple<Guid, FrozenSet<ILarkComponent>>> GetEntitiesWithComponentsSync(params Type[] componentTypes) {
    var hasGeneric = componentTypes.Any(t => t.IsGenericType);
    foreach (var (key, components) in entities) {

      if (!hasGeneric) {
        if (entityComponents[key].IsSupersetOf(componentTypes)) {
          yield return new(key, components);
        }
      }

      // If componentTypes has any generic types, we need to check the inheritance chain.
      if (hasGeneric) {
        foreach (var componentType in componentTypes) {
          if (componentType.IsGenericType) {
            // EntityCompoents will have full type records like Typeof(CastAbility<HeroMainAttack>), typeof(CastAbility<HeroAltAttack>), etc. In this example if we are looking for CastAbility<>, we need to check for all types that inherit from CastAbility<>.
            if (entityComponents[key].Any(t => t.IsGenericType && (t.GetGenericTypeDefinition() == componentType || t.IsAssignableFrom(componentType)))) {
              yield return new(key, components);
            }
          }
        }
      }
    }
  }

  public async IAsyncEnumerable<ValueTuple<Guid, FrozenSet<ILarkComponent>>> GetEntitiesWithComponents(params Type[] componentTypes) {
    var hasGeneric = componentTypes.Any(t => t.IsGenericType);
    foreach (var (key, components) in entities) {

      if (!hasGeneric) {
        if (entityComponents[key].IsSupersetOf(componentTypes)) {
          yield return new(key, components);
        }
      }

      // If componentTypes has any generic types, we need to check the inheritance chain.
      if (hasGeneric) {
        foreach (var componentType in componentTypes) {
          if (componentType.IsGenericType) {
            // EntityCompoents will have full type records like Typeof(CastAbility<HeroMainAttack>), typeof(CastAbility<HeroAltAttack>), etc. In this example if we are looking for CastAbility<>, we need to check for all types that inherit from CastAbility<>.
            if (entityComponents[key].Any(t => t.IsGenericType && (t.GetGenericTypeDefinition() == componentType || t.IsAssignableFrom(componentType)))) {
              yield return new(key, components);
            }
          }
        }
      }
    }
    await Task.CompletedTask;
  }

  // public IEnumerable<ValueTuple<Guid, FrozenSet<ILarkComponent>>> GetEntitiesWithComponentsSync(params Type[] componentTypes) {
  //   foreach (var (key, components) in entities) {
  //     if (entityComponents[key].IsSupersetOf(componentTypes)) {
  //       yield return new(key, components);
  //     }
  //   }
  // }

  // GetEntityIdsWithComponents: Get the ids of all entities that have all of the specified components.
  public IEnumerable<Guid> GetEntityIdsWithComponents(params Type[] componentTypes) {
    var componentTypeSet = new HashSet<Type>(componentTypes);
    foreach (var (key, components) in entities) {
      if (entityComponents[key].IsSupersetOf(componentTypeSet)) {
        yield return key;
      }
    }
  }

  public ValueTuple<Guid, FrozenSet<ILarkComponent>> GetEntityWithPredicate(Func<FrozenSet<ILarkComponent>, bool> predicate) {
    foreach (var (key, components) in entities) {
      if (predicate(components)) {
        return new(key, components);
      }
    }

    throw new Exception("No entity found with specified components");
  }

  public void RemoveEntity(Guid key) {
    if (!entities.TryGetValue(key, out var value)) {
      throw new Exception("Entity does not exist");
    }

    foreach (var component in value) {
      var componentType = component.GetType();
      entitiesByComponentType[componentType].Remove(key);
    }
    entityComponents.TryRemove(key, out _);
    entities.TryRemove(key, out _);
  }

  public void UpdateEntityComponent<TComp>(Guid key, TComp component) where TComp : ILarkComponent {
    if (!entities.TryGetValue(key, out var value)) {
      throw new Exception("Entity does not exist");
    }

    var componentType = typeof(TComp);
    entitiesByComponentType[componentType].Remove(key);
    entitiesByComponentType[componentType].Add(key);

    var newSet = value.Where(c => c.GetType() != componentType).ToHashSet();
    newSet.Add(component);
    entities[key] = newSet.ToFrozenSet();
  }

  public void RemoveEntityComponent<TComp>(Guid key) where TComp : ILarkComponent {
    if (!entities.TryGetValue(key, out var value)) {
      throw new Exception("Entity does not exist");
    }

    var componentType = typeof(TComp);
    entitiesByComponentType[componentType].Remove(key);

    var ecSet = entityComponents[key].ToHashSet();
    ecSet.Remove(componentType);
    entityComponents[key] = ecSet.ToFrozenSet();

    var newSet = value.Where(c => c.GetType() != componentType).ToHashSet();
    entities[key] = newSet.ToFrozenSet();
  }

  // RemoveEntityComponent: Remove a component from an entity that matches a predicate.
  public void RemoveEntityComponent(Guid key, Func<ILarkComponent, bool> predicate) {
    if (!entities.TryGetValue(key, out var value)) {
      throw new Exception("Entity does not exist");
    }

    var matchingComponents = value.Where(predicate).ToHashSet();
    foreach (var component in matchingComponents) {
      var componentType = component.GetType();
      entitiesByComponentType[componentType].Remove(key);
    }

    var newSet = value.Where(c => !matchingComponents.Contains(c)).ToHashSet();
    entities[key] = newSet.ToFrozenSet();
  }

  public void AddEntityComponent<TComp>(Guid key, TComp component) where TComp : ILarkComponent {
    if (!entities.TryGetValue(key, out var value)) {
      throw new Exception("Entity does not exist");
    }

    // Use GetType rather than typeof(TComp) to handle generic types.
    var componentType = component.GetType();
    if (!entitiesByComponentType.TryGetValue(componentType, out var entitySet)) {
      entitiesByComponentType.TryAdd(componentType, []);
    }

    entitiesByComponentType[componentType].Add(key);

    var eSet = value.ToHashSet();
    eSet.Add(component);
    entities[key] = eSet.ToFrozenSet();

    // Add to entityComponents
    var ecSet = entityComponents[key].ToHashSet();
    ecSet.Add(componentType);
    entityComponents[key] = ecSet.ToFrozenSet();
  }
}
