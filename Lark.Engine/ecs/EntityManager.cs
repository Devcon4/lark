namespace Lark.Engine.ecs;

public class EntityManager {

  private Dictionary<Guid, HashSet<ILarkComponent>> entities = new();

  private Dictionary<Type, HashSet<Guid>> entitiesByComponentType = new();

  public Guid AddEntity(params ILarkComponent[] components) {
    Guid key = Guid.NewGuid();
    return AddEntity(key, components);
  }

  public Guid AddEntity(Guid key, params ILarkComponent[] components) {

    if (entities.ContainsKey(key)) {
      throw new Exception($"Entity {key} already exists");
    }

    var componentTypes = components.Select(c => c.GetType()).ToHashSet();
    if (componentTypes.Count != components.Length) {
      throw new Exception($"Components must be unique on entity: {key}");
    }

    entities.Add(key, components.ToHashSet());
    foreach (var component in components) {
      var componentType = component.GetType();

      if (!entitiesByComponentType.TryGetValue(componentType, out HashSet<Guid>? value)) {
        value = new HashSet<Guid>();
        entitiesByComponentType.Add(componentType, value);
      }

      value.Add(key);
    }

    return key;
  }

  // GetTotalNumberOfEntities
  public int GetEntitiesCount() {
    return entities.Count;
  }

  public ValueTuple<Guid, HashSet<ILarkComponent>> GetEntity(Guid key) {
    return new ValueTuple<Guid, HashSet<ILarkComponent>>(key, entities[key]);
  }

  public HashSet<ValueTuple<Guid, HashSet<ILarkComponent>>> GetEntitiesWithComponents(params Type[] componentTypes) {
    var entitiesWithComponents = new List<ValueTuple<Guid, HashSet<ILarkComponent>>>();
    var componentTypeSet = new HashSet<Type>(componentTypes);

    foreach (var entity in entities) {
      var entityComponents = entity.Value;
      var entityComponentTypes = new HashSet<Type>(entityComponents.Select(c => c.GetType()));

      if (entityComponentTypes.IsSupersetOf(componentTypeSet)) {
        entitiesWithComponents.Add(new(entity.Key, entityComponents));
      }
    }

    return entitiesWithComponents.ToHashSet();
  }

  public void RemoveEntity(Guid key) {
    if (!entities.TryGetValue(key, out HashSet<ILarkComponent>? value)) {
      throw new Exception("Entity does not exist");
    }

    foreach (var component in value) {
      var componentType = component.GetType();
      entitiesByComponentType[componentType].Remove(key);
    }

    entities.Remove(key);
  }

  public void UpdateEntityComponent(Guid key, ILarkComponent component) {
    if (!entities.TryGetValue(key, out HashSet<ILarkComponent>? value)) {
      throw new Exception("Entity does not exist");
    }

    var componentType = component.GetType();
    entitiesByComponentType[componentType].Remove(key);
    entitiesByComponentType[componentType].Add(key);

    value.RemoveWhere(c => c.GetType() == componentType);
    value.Add(component);
  }
}
