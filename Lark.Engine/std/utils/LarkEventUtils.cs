using System.Collections.Frozen;
using Silk.NET.OpenGL;

namespace Lark.Engine.std;

public static partial class LarkUtils {
  // AnyPressed
  public static bool AnyKeyPressed<T>(FrozenSet<T> events) where T : ILarkInput {
    return events.Any(e => e is LarkKeyEvent keyEvent && keyEvent.Action == LarkInputAction.Press);
  }

  public static bool AnyMousePressed<T>(FrozenSet<T> events) where T : ILarkInput {
    return events.Any(e => e is LarkMouseEvent mouseEvent && mouseEvent.Action == LarkInputAction.Press);
  }

  // IsPressed
  public static bool IsKeyPressed<T>(FrozenSet<T> events, LarkKeys key, LarkKeyModifiers? mods = null) where T : ILarkInput {
    return events.Any(e => e is LarkKeyEvent keyEvent && keyEvent.Action == LarkInputAction.Press && keyEvent.Key == key && (mods is null || keyEvent.Mods == mods));
  }
  public static bool IsMousePressed<T>(FrozenSet<T> events, LarkMouseButton button, LarkKeyModifiers? mods = null) where T : ILarkInput {
    return events.Any(e => e is LarkMouseEvent mouseEvent && mouseEvent.Action == LarkInputAction.Press && mouseEvent.Button == button && (mods is null || mouseEvent.Mods == mods));
  }

  // AnyReleased
  public static bool AnyKeyReleased<T>(FrozenSet<T> events) where T : ILarkInput {
    return events.Any(e => e is LarkKeyEvent keyEvent && keyEvent.Action == LarkInputAction.Release);
  }
  public static bool AnyMouseReleased<T>(FrozenSet<T> events) where T : ILarkInput {
    return events.Any(e => e is LarkMouseEvent mouseEvent && mouseEvent.Action == LarkInputAction.Release);
  }

  // IsReleased
  public static bool IsKeyReleased<T>(FrozenSet<T> events, LarkKeys key, LarkKeyModifiers? mods = null) where T : ILarkInput {
    return events.Any(e => e is LarkKeyEvent keyEvent && keyEvent.Action == LarkInputAction.Release && keyEvent.Key == key && (mods is null || keyEvent.Mods == mods));
  }
  public static bool IsMouseReleased<T>(FrozenSet<T> events, LarkMouseButton button, LarkKeyModifiers? mods = null) where T : ILarkInput {
    return events.Any(e => e is LarkMouseEvent mouseEvent && mouseEvent.Action == LarkInputAction.Release && mouseEvent.Button == button && (mods is null || mouseEvent.Mods == mods));
  }

  // AnyHeld
  public static bool AnyKeyHeld<T>(FrozenSet<T> events) where T : ILarkInput {
    return events.Any(e => e is LarkKeyEvent keyEvent && keyEvent.Action == LarkInputAction.Hold);
  }
  public static bool AnyMouseHeld<T>(FrozenSet<T> events) where T : ILarkInput {
    return events.Any(e => e is LarkMouseEvent mouseEvent && mouseEvent.Action == LarkInputAction.Hold);
  }

  // IsHeld
  public static bool IsKeyHeld<T>(FrozenSet<T> events, LarkKeys key, LarkKeyModifiers? mods = null) where T : ILarkInput {
    return events.Any(e => e is LarkKeyEvent keyEvent && keyEvent.Action == LarkInputAction.Hold && keyEvent.Key == key && (mods is null || keyEvent.Mods == mods));
  }
  public static bool IsMouseHeld<T>(FrozenSet<T> events, LarkMouseButton button, LarkKeyModifiers? mods = null) where T : ILarkInput {
    return events.Any(e => e is LarkMouseEvent mouseEvent && mouseEvent.Action == LarkInputAction.Hold && mouseEvent.Button == button && (mods is null || mouseEvent.Mods == mods));
  }

  public static bool AnyEvent<T>(FrozenSet<ILarkInput> events, out T? e) where T : ILarkInput {
    e = (T?)events.FirstOrDefault(e => e is T, null);
    return e is not null;
  }

}