using System.Collections.Frozen;
using Lark.Engine.ecs;

namespace Lark.Engine.std;
using ActionName = System.String;

public record struct InputMapComponent: ILarkComponent {}

public record struct ActionMapComponent(ActionName ActionName, ILarkActionTrigger Trigger): ILarkComponent {
  public Action<ActionName, ValueTuple<Guid, FrozenSet<ILarkComponent>>, ILarkInput> Action { get; init; }
}

public class InputSystem(EntityManager em, InputManager im, LarkWindow window) : LarkSystem {
  public override Type[] RequiredComponents => [
    typeof(SystemComponent),
    typeof(InputMapComponent),
    typeof(CurrentKeyInputComponent),
    typeof(CurrentMouseInputComponent),
    typeof(CurrentCursorInputComponent),
    typeof(CurrentScrollInputComponent)
    ];

  public override Task Init() {
    em.AddEntity(
      new MetadataComponent("Input"),
      new SystemComponent(),
      new InputMapComponent(),
      new CurrentKeyInputComponent(),
      new CurrentMouseInputComponent(),
      new CurrentCursorInputComponent(),
      new CurrentScrollInputComponent());

    window.SetKeyCallback(im.KeyCallbackAction);
    window.SetMouseButtonCallback(im.MouseButtonCallbackAction);
    window.SetCursorPosCallback(im.CursorPosCallbackAction);
    window.SetScrollCallback(im.ScrollCallbackAction);

    return Task.CompletedTask;
  }

  public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
  }

  public override void BeforeUpdate() {
    var (key, components) = em.GetEntity(typeof(SystemComponent),
    typeof(CurrentKeyInputComponent),
    typeof(CurrentMouseInputComponent),
    typeof(CurrentCursorInputComponent),
    typeof(CurrentScrollInputComponent));

    em.UpdateEntityComponent(key, new CurrentKeyInputComponent());
    em.UpdateEntityComponent(key, new CurrentMouseInputComponent());
    em.UpdateEntityComponent(key, new CurrentCursorInputComponent());
    em.UpdateEntityComponent(key, new CurrentScrollInputComponent());
  }
}