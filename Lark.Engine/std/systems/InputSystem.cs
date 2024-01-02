using System.Collections.Frozen;
using Lark.Engine.ecs;

namespace Lark.Engine.std;
using ActionName = System.String;

public record struct InputEntityMarker : ILarkComponent { }

public record struct ActionComponent(ActionName ActionName, Action<ValueTuple<Guid, FrozenSet<ILarkComponent>>, ILarkInput> Callback) : ILarkComponent { }

public class InputSystem(EntityManager em, InputManager im, LarkWindow window) : LarkSystem {
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

  public override void BeforeUpdate() {
    var (key, components) = em.GetEntity(InputManager.InputEntity);

    em.UpdateEntityComponent(key, new CurrentMouseInputComponent());
    // em.UpdateEntityComponent(key, new CurrentCursorInputComponent());
    em.UpdateEntityComponent(key, new CurrentScrollInputComponent());
  }
}