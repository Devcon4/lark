using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;

namespace Lark.Engine.ecs;

public class EntityManager(ILogger<EntityManager> logger) {

  private Dictionary<Guid, FrozenSet<ILarkComponent>> entities = new();
  private Dictionary<Guid, FrozenSet<Type>> entityComponents = new();

  private Dictionary<Type, HashSet<Guid>> entitiesByComponentType = new();

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

  public ValueTuple<Guid, FrozenSet<ILarkComponent>> GetEntity(Guid key) {
    return new ValueTuple<Guid, FrozenSet<ILarkComponent>>(key, entities[key].ToFrozenSet());
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

  public async IAsyncEnumerable<ValueTuple<Guid, FrozenSet<ILarkComponent>>> GetEntitiesWithComponents(params Type[] componentTypes) {
    foreach (var (key, components) in entities) {
      if (entityComponents[key].IsSupersetOf(componentTypes)) {
        yield return new(key, components);
      }
    }
    await Task.CompletedTask;
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
    entityComponents.Remove(key);
    entities.Remove(key);
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

    var componentType = typeof(TComp);
    if (!entitiesByComponentType.TryGetValue(componentType, out var entitySet)) {
      entitiesByComponentType.Add(componentType, []);
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
