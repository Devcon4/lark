using System.Collections.Frozen;
using Silk.NET.OpenGL;

namespace Lark.Engine.std;

public static partial class LarkUtils {
  public static bool AnyKeyPressed<T>(FrozenSet<T> events) where T : ILarkInput {
    return events.Any(e => e is LarkKeyEvent keyEvent && keyEvent.Action == LarkInputAction.Press);
  }

  public static bool IsKeyPressed<T>(FrozenSet<T> events, LarkKeys key, LarkKeyModifiers? mods = null) where T : ILarkInput {
    return events.Any(e => e is LarkKeyEvent keyEvent && keyEvent.Action == LarkInputAction.Press && keyEvent.Key == key && (mods is null || keyEvent.Mods == mods));
  }

  public static bool AnyKeyReleased<T>(FrozenSet<T> events) where T : ILarkInput {
    return events.Any(e => e is LarkKeyEvent keyEvent && keyEvent.Action == LarkInputAction.Release);
  }

  public static bool IsKeyReleased<T>(FrozenSet<T> events, LarkKeys key, LarkKeyModifiers? mods = null) where T : ILarkInput {
    return events.Any(e => e is LarkKeyEvent keyEvent && keyEvent.Action == LarkInputAction.Release && keyEvent.Key == key && (mods is null || keyEvent.Mods == mods));
  }

  public static bool AnyKeyHeld<T>(FrozenSet<T> events) where T : ILarkInput {
    return events.Any(e => e is LarkKeyEvent keyEvent && keyEvent.Action == LarkInputAction.Hold);
  }

  public static bool IsKeyHeld<T>(FrozenSet<T> events, LarkKeys key, LarkKeyModifiers? mods = null) where T : ILarkInput {
    return events.Any(e => e is LarkKeyEvent keyEvent && keyEvent.Action == LarkInputAction.Hold && keyEvent.Key == key && (mods is null || keyEvent.Mods == mods));
  }

  public static bool AnyEvent<T>(FrozenSet<ILarkInput> events, out T? e) where T : ILarkInput {
    e = (T?)events.FirstOrDefault(e => e is T, null);
    return e is not null;
  }

}