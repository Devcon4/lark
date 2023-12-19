
using System.Collections.Frozen;
using Lark.Engine.ecs;

namespace Lark.Engine.std;

public record struct ActionMapComponent(string ActionName, ILarkActionTrigger trigger) : ILarkComponent { }

// TODO: Rework this so rather than being a system it uses the straight input callbacks.
// I want actions to not be linked to framerate. 
// Action maps also need to be reworked. I should be able to pass a name and action func and seperately associate an action to a trigger.
// That will allow the triggers to be swapped out or remapped.
public class ActionSystem(EntityManager em) : LarkSystem {
  public override Type[] RequiredComponents => [typeof(ActionComponent)];

  public override Task Init() {
    em.AddEntity(new MetadataComponent("ActionMap"), new ActionMapComponent("MoveForward", new LarkKeyTrigger(LarkKeys.W)));
    return Task.CompletedTask;
  }

  public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
    var (key, components) = Entity;
    var actionMap = components.Get<ActionMapComponent>();

    var (id, input) = em.GetEntity(InputManager.InputEntity);
    var (keyInput, mouseInput, cursorInput, scrollInput) = input.Get<CurrentKeyInputComponent, CurrentMouseInputComponent, CurrentCursorInputComponent, CurrentScrollInputComponent>();

    if (actionMap.Trigger.Check(keyInput)) {
      actionMap.Action(actionMap.ActionName, Entity, keyInput);
    }

    if (actionMap.Trigger.Check(mouseInput)) {
      actionMap.Action(actionMap.ActionName, Entity, mouseInput);
    }

    if (actionMap.Trigger.Check(cursorInput)) {
      actionMap.Action(actionMap.ActionName, Entity, cursorInput);
    }

    if (actionMap.Trigger.Check(scrollInput)) {
      actionMap.Action(actionMap.ActionName, Entity, scrollInput);
    }

  }
}