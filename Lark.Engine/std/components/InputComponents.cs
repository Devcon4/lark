
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

public record struct PressedKey(LarkKeys Key, LarkKeyModifiers? Mods = null) : ILarkKeyInput {
  public LarkKeys Key { get; init; } = Key;
  public LarkKeyModifiers? Mods { get; init; } = Mods;
}

public record struct CurrentKeysInputComponent() : ILarkComponent {

  public FrozenSet<PressedKey> Keys { get; init; } = FrozenSet<PressedKey>.Empty;

  // public readonly ValueTuple<LarkKeys, LarkKeyModifiers?> KeyMods => new(Key, Mods);
  // public readonly ValueTuple<LarkKeys, LarkInputAction?> KeyActions => new(Key, Action);
}

// MouseButton
public interface ILarkMouseInput : ILarkInput {
  public LarkMouseButton Button { get; init; }
  public LarkInputAction Action { get; init; }
  public LarkKeyModifiers Mods { get; init; }
}

public record struct LarkMouseTrigger : ILarkActionTrigger, ILarkMouseInput {
  public LarkMouseButton Button { get; init; }
  public LarkInputAction Action { get; init; }
  public LarkKeyModifiers Mods { get; init; }

  public bool Check(ILarkInput input) {
    if (input is not ILarkMouseInput mouseInput) return false;
    return mouseInput.Button == Button && mouseInput.Action == Action && mouseInput.Mods == Mods;
  }
}

public record struct CurrentMouseInputComponent : ILarkComponent, ILarkMouseInput {
  public LarkMouseButton Button { get; init; }
  public LarkInputAction Action { get; init; }
  public LarkKeyModifiers Mods { get; init; }
}

// Cursor
public interface ILarkCursorInput : ILarkInput {
  public Vector2 Position { get; init; }
}

public record struct LarkCursorTrigger : ILarkActionTrigger, ILarkCursorInput {
  public Vector2 Position { get; init; }

  public bool Check(ILarkInput input) {
    if (input is not ILarkCursorInput cursorAction) return false;
    return true;
  }
}

public record struct CurrentCursorInputComponent : ILarkComponent, ILarkCursorInput {
  public Vector2 Position { get; init; }
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

public record struct CurrentScrollInputComponent : ILarkComponent, ILarkScrollInput {
  public Vector2 Offset { get; init; }
}