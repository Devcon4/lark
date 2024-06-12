
using System.Collections.Frozen;
using System.Numerics;
using Lark.Engine.ecs;

namespace Lark.Engine.std;

public interface ILarkActionTrigger {
  public bool Check(ILarkInput trigger);
}

public interface ILarkInput { }

// KeyButton
public interface ILarkKeyInput : ILarkInput {
  public LarkKeys Key { get; init; }
  public LarkKeyModifiers? Mods { get; init; }
}

public interface ILarkInputAction {
  public LarkInputAction Action { get; init; }
}

public record struct LarkKeyTrigger : ILarkActionTrigger, ILarkKeyInput {
  public LarkKeys Key { get; init; }
  public LarkKeyModifiers? Mods { get; init; }

  public LarkKeyTrigger(LarkKeys key, LarkKeyModifiers? mods = null) {
    Key = key;
    Mods = mods;
  }

  public bool Check(ILarkInput input) {
    if (input is not ILarkKeyInput keyInput) return false;

    return keyInput.Key == Key &&
      (Mods is null || keyInput.Mods == Mods);
  }
}

public record struct LarkMultiTrigger : ILarkActionTrigger {
  public FrozenSet<ILarkActionTrigger> Triggers { get; init; }

  public LarkMultiTrigger(IEnumerable<ILarkActionTrigger> triggers) {
    Triggers = triggers.ToFrozenSet();
  }

  public bool Check(ILarkInput input) {
    if (input is not ILarkKeyInput keyInput) return false;

    foreach (var trigger in Triggers) {
      if (trigger.Check(keyInput)) return true;
    }

    return false;
  }
}

public record struct LarkKeyEvent(LarkKeys Key, LarkInputAction Action, LarkKeyModifiers? Mods = null) : ILarkKeyInput, ILarkInputAction {
  public LarkKeys Key { get; init; } = Key;
  public LarkInputAction Action { get; init; } = Action;
  public LarkKeyModifiers? Mods { get; init; } = Mods;

}

public record struct CurrentKeysInputComponent() : ILarkComponent {

  public FrozenSet<LarkKeyEvent> Events { get; init; } = FrozenSet<LarkKeyEvent>.Empty;

  // public readonly ValueTuple<LarkKeys, LarkKeyModifiers?> KeyMods => new(Key, Mods);
  // public readonly ValueTuple<LarkKeys, LarkInputAction?> KeyActions => new(Key, Action);
}

// MouseButton
public interface ILarkMouseInput : ILarkInput {
  public LarkMouseButton Button { get; init; }
  public LarkKeyModifiers? Mods { get; init; }
}

public record struct LarkMouseEvent(LarkMouseButton Button, LarkInputAction Action, LarkKeyModifiers? Mods = null) : ILarkMouseInput, ILarkInputAction {
  public LarkMouseButton Button { get; init; } = Button;
  public LarkInputAction Action { get; init; } = Action;
  public LarkKeyModifiers? Mods { get; init; } = Mods;
}

public record struct LarkMouseTrigger : ILarkActionTrigger, ILarkMouseInput {
  public LarkMouseButton Button { get; init; }
  public LarkKeyModifiers? Mods { get; init; }

  public LarkMouseTrigger(LarkMouseButton button, LarkKeyModifiers? mods = null) {
    Button = button;
    Mods = mods;
  }

  public bool Check(ILarkInput input) {
    if (input is not ILarkMouseInput mouseInput) return false;
    return mouseInput.Button == Button && mouseInput.Mods == Mods;
  }
}

public record struct CurrentMouseInputComponent() : ILarkComponent {
  public FrozenSet<LarkMouseEvent> Events { get; init; } = FrozenSet<LarkMouseEvent>.Empty;
}

// Cursor
public interface ILarkCursorInput : ILarkInput {
  public Vector2 Position { get; init; }
}

public record struct LarkCursorTrigger : ILarkActionTrigger, ILarkCursorInput {
  public Vector2 Position { get; init; }

  public bool Check(ILarkInput input) {
    if (input is ILarkCursorInput cursorAction) return true;
    return false;
  }
}

public record struct LarkCursorEvent(Vector2 Position) : ILarkCursorInput {
  public Vector2 Position { get; init; } = Position;
}

public record struct CurrentCursorInputComponent() : ILarkComponent {
  public FrozenSet<LarkCursorEvent> Events { get; init; } = FrozenSet<LarkCursorEvent>.Empty;

}

// Scroll
public interface ILarkScrollInput : ILarkInput {
  public Vector2 Offset { get; init; }
}

public record struct LarkScrollTrigger : ILarkActionTrigger, ILarkScrollInput {
  public Vector2 Offset { get; init; }

  public bool Check(ILarkInput input) {
    if (input is not ILarkScrollInput scrollAction) return false;
    return true;
  }
}

public record struct LarkScrollEvent(Vector2 Offset) : ILarkScrollInput {
  public Vector2 Offset { get; init; } = Offset;
}

public record struct CurrentScrollInputComponent() : ILarkComponent {
  public FrozenSet<LarkScrollEvent> Events { get; init; } = FrozenSet<LarkScrollEvent>.Empty;
}