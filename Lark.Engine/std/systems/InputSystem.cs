using System.Collections.Frozen;
using Lark.Engine.ecs;
using Microsoft.Extensions.Logging;

namespace Lark.Engine.std;
using ActionName = System.String;

public record struct InputEntityMarker : ILarkComponent { }

public record struct ActionComponent(ActionName ActionName, Action<ValueTuple<Guid, FrozenSet<ILarkComponent>>, FrozenSet<ILarkInput>> Callback) : ILarkComponent { }

public class InputSystem(EntityManager em, InputManager im, LarkWindow window, ILogger<InputSystem> logger) : LarkSystem {
  public override Type[] RequiredComponents => [
    typeof(SystemComponent),
    typeof(InputEntityMarker)
    ];

  public override Task Init() {
    em.AddEntity(
      new MetadataComponent("Input"),
      new SystemComponent(),
      new InputEntityMarker(),
      new CurrentKeysInputComponent(),
      new CurrentMouseInputComponent(),
      new CurrentCursorInputComponent(),
      new CurrentScrollInputComponent());

    em.AddEntity(new MetadataComponent("ActionMap"), new SystemComponent(), new LarkMapComponent(ActionManager.DefaultMap, true, []));

    window.SetKeyCallback(im.KeyCallbackAction);
    window.SetMouseButtonCallback(im.MouseButtonCallbackAction);
    window.SetCursorPosCallback(im.CursorPosCallbackAction);
    window.SetScrollCallback(im.ScrollCallbackAction);

    return Task.CompletedTask;
  }

  public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
  }

  public override void AfterUpdate() {
    var (key, components) = em.GetEntity(InputManager.InputEntity);

    // Get keyInput.Events. For all pressed key, update them to held events. For all released keys, remove matching held events.
    var keyInput = components.Get<CurrentKeysInputComponent>();

    FrozenSet<LarkKeyEvent> events = keyInput.Events;
    List<LarkKeyEvent> newEvents = [];

    foreach (var e in keyInput.Events) {
      if (LarkUtils.IsKeyPressed(events, e.Key, e.Mods) && !LarkUtils.IsKeyReleased(events, e.Key, e.Mods)) {
        newEvents.Add(e with { Action = LarkInputAction.Hold });
        continue;
      }

      if (LarkUtils.IsKeyHeld(events, e.Key, e.Mods) && !LarkUtils.IsKeyReleased(events, e.Key, e.Mods)) {
        newEvents.Add(e);
        continue;
      }

    }

    // TODO: This is not working as expected. Need to fix.

    // If the window is not focused, clear all events. Send a release event for all held keys.
    // if (!window.IsFocused) {
    //   newEvents = newEvents.Select(e => e with { Action = LarkInputAction.Release }).ToList();
    // }

    em.UpdateEntityComponent(key, new CurrentKeysInputComponent() with { Events = newEvents.ToFrozenSet() });

    em.UpdateEntityComponent(key, new CurrentMouseInputComponent());
    em.UpdateEntityComponent(key, new CurrentCursorInputComponent());
    em.UpdateEntityComponent(key, new CurrentScrollInputComponent());
  }
}