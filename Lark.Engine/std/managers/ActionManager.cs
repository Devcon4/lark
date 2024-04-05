using System.Collections.Frozen;
using System.Diagnostics;
using Lark.Engine.ecs;
using Microsoft.Extensions.Logging;
using Silk.NET.OpenGL;

namespace Lark.Engine.std;

public class ActionModule(ActionManager am) : ILarkModule {
  public Task Cleanup() {
    return Task.CompletedTask;
  }

  public Task Init() {
    return Task.CompletedTask;
  }

  public async Task Run() {
    await am.UpdateAsync();
  }
}

public class ActionManager(EntityManager em, ILogger<ActionManager> logger) : LarkManager {
  public static Type[] ActionMapEntity => [typeof(SystemComponent), typeof(LarkMapComponent)];
  public static string DefaultMap => "Default";

  public override Task Init() {
    em.AddEntity(new MetadataComponent("ActionMap"), new SystemComponent(), new LarkMapComponent(DefaultMap, true, []));

    return Task.CompletedTask;
  }

  public void CreateActionMap(string mapName) {
    var (id, _) = em.GetEntity(ActionMapEntity);
    em.AddEntityComponent(id, new LarkMapComponent(mapName, false, []));
  }

  public void RemoveActionMap(string mapName) {
    var (id, _) = em.GetEntity(ActionMapEntity);
    em.RemoveEntityComponent(id, c => c is LarkMapComponent map && map.MapName == mapName);
  }

  // RenameActionMap

  public void RenameActionMap(string oldName, string newName) {
    var (id, components) = em.GetEntity(ActionMapEntity);

    LarkMapComponent? existing = components.GetList<LarkMapComponent>().FirstOrDefault(c => c.MapName == newName);
    if (existing is null) {
      throw new Exception($"Action map {newName} already exists");
    }

    LarkMapComponent? old = components.GetList<LarkMapComponent>().FirstOrDefault(c => c.MapName == oldName);
    if (old is null) {
      throw new Exception($"Action map {oldName} does not exist");
    }

    em.UpdateEntityComponent(id, (LarkMapComponent)old with {
      MapName = newName
    });
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
  // TODO: this method seems wrong. Check that it actually switches the active map.
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

  public Task UpdateAsync() {
    foreach (var entity in em.GetEntitiesWithComponentsSync(typeof(ActionComponent), typeof(MetadataComponent))) {
      var (key, components) = entity;
      var name = components.Get<MetadataComponent>().Name;
      var (_, mapComponents) = em.GetEntity(ActionMapEntity);
      var actionMap = mapComponents.GetList<LarkMapComponent>().FirstOrDefault(m => m.Active);
      var actions = components.GetList<ActionComponent>();

      foreach (var action in actions) {
        actionMap.Bindings.TryGetValue(action.ActionName, out ILarkActionTrigger? trigger);

        if (trigger is null) {
          logger.LogWarning("Action {actionName} does not exist in map {mapName}", action.ActionName, actionMap.MapName);
          continue;
        }

        var (id, input) = em.GetEntity(InputManager.InputEntity);
        var (keyInputs, mouseInputs, cursorInputs, scrollInputs) = input.Get<CurrentKeysInputComponent, CurrentMouseInputComponent, CurrentCursorInputComponent, CurrentScrollInputComponent>();

        // If there are any events log them.
        // if (keyInputs.Events.Any() || mouseInputs.Events.Any() || cursorInputs.Events.Any() || scrollInputs.Events.Any()) {
        //   logger.LogInformation("Events :: {events}", [.. keyInputs.Events, .. mouseInputs.Events, .. cursorInputs.Events, .. scrollInputs.Events]);
        // }

        //TODO: Don't love all these Where calls, look at optimizing this.
        var triggeredKeyEvents = keyInputs.Events.Where(k => trigger.Check(k)).Cast<ILarkInput>();
        if (triggeredKeyEvents.Any()) {
          action.Callback(entity, triggeredKeyEvents.ToFrozenSet());
        }

        var triggeredMouseEvents = mouseInputs.Events.Where(k => trigger.Check(k)).Cast<ILarkInput>();
        if (triggeredMouseEvents.Any()) {
          action.Callback(entity, triggeredMouseEvents.ToFrozenSet());
        }

        var triggeredCursorEvents = cursorInputs.Events.Where(k => trigger.Check(k)).Cast<ILarkInput>();
        if (triggeredCursorEvents.Any()) {
          action.Callback(entity, triggeredCursorEvents.ToFrozenSet());
        }

        var triggeredScrollEvents = scrollInputs.Events.Where(k => trigger.Check(k)).Cast<ILarkInput>();
        if (triggeredScrollEvents.Any()) {
          action.Callback(entity, triggeredScrollEvents.ToFrozenSet());
        }
      }
    }

    return Task.CompletedTask;
  }
}

public record struct LarkMapComponent(string MapName, bool Active, Dictionary<string, ILarkActionTrigger> Bindings) : ILarkComponent { }