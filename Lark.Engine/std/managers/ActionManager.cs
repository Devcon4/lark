using Lark.Engine.ecs;

namespace Lark.Engine.std;
public class ActionManager(EntityManager em) {
  public static Type[] ActionMapEntity => [typeof(SystemComponent), typeof(LarkMapComponent)];

  public void CreateActionMap(string mapName) {
    em.AddEntity(new LarkMapComponent(mapName, false, []));
  }

  public void AddActionToMap(string mapName, string actionName, ILarkActionTrigger trigger) {
    var (key, components) = em.GetEntity(ActionMapEntity);
    var map = components.Get<LarkMapComponent>();

    if (map.Bindings.ContainsKey(actionName)) {
      throw new Exception($"Action {actionName} already exists in map {mapName}");
    }

    var newBindings = map.Bindings;
    newBindings.Add(actionName, trigger);

    em.UpdateEntityComponent(key, map with {
      Bindings = newBindings
    });
  }

  public void RemoveActionFromMap(string mapName, string actionName) {
    var (key, components) = em.GetEntity(ActionMapEntity);
    var map = components.Get<LarkMapComponent>();

    if (!map.Bindings.ContainsKey(actionName)) {
      throw new Exception($"Action {actionName} does not exist in map {mapName}");
    }

    var newBindings = map.Bindings;
    newBindings.Remove(actionName);

    em.UpdateEntityComponent(key, map with {
      Bindings = newBindings
    });
  }

  // SwitchActiveMap

  public void SwitchActiveMap(string mapName) {
    var (key, components) = em.GetEntity(typeof(LarkMapComponent));
    var map = components.Get<LarkMapComponent>();

    if (map.MapName == mapName) {
      return;
    }

    em.UpdateEntityComponent(key, map with {
      Active = false
    });

    var (newKey, newComponents) = em.GetEntity(typeof(LarkMapComponent));
    var newMap = newComponents.Get<LarkMapComponent>();

    em.UpdateEntityComponent(newKey, newMap with {
      Active = true
    });
  }
}

public record struct LarkMapComponent(string MapName, bool Active, Dictionary<string, ILarkActionTrigger> Bindings) : ILarkComponent { }