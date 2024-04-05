using System.Collections.Frozen;
using System.Collections.Immutable;
using Lark.Engine.std;

namespace Lark.Engine.ecs;

public static class HashSetExtensions {

  // GetList: Get all components of a type
  public static ImmutableList<TComponent> GetList<TComponent>(this FrozenSet<ILarkComponent> hashSet) where TComponent : ILarkComponent {
    return hashSet.Where(c => c.GetType() == typeof(TComponent) || c.GetType().IsSubclassOf(typeof(TComponent)) || typeof(TComponent).IsInterface && typeof(TComponent).IsAssignableFrom(c.GetType())).Select(c => (TComponent)c).ToImmutableList();
  }

  public static ImmutableList<dynamic> GetList<TComponent>(this FrozenSet<ILarkComponent> hashSet, Type type) where TComponent : ILarkComponent {
    return hashSet.Where(c => c.GetType() == type || c.GetType().IsGenericType && (c.GetType().GetGenericTypeDefinition() == type || c.GetType().GetGenericTypeDefinition().IsAssignableFrom(type))).Select(c => (dynamic)c).ToImmutableList();
  }

  public static TComponent Get<TComponent>(this FrozenSet<ILarkComponent> hashSet) where TComponent : ILarkComponent {
    return (TComponent)hashSet.First(c => c.GetType() == typeof(TComponent) || c.GetType().IsSubclassOf(typeof(TComponent)) || typeof(TComponent).IsInterface && typeof(TComponent).IsAssignableFrom(c.GetType()));
  }

  public static dynamic Get<TComponent>(this FrozenSet<ILarkComponent> hashSet, Type type) where TComponent : ILarkComponent {
    return hashSet.First(c => c.GetType() == type || c.GetType().IsGenericType && (c.GetType().GetGenericTypeDefinition() == type || c.GetType().GetGenericTypeDefinition().IsAssignableFrom(type)));
  }

  // TryGet: Get a component of a type, return bool, out TComponent
  public static bool TryGet<TComponent>(this FrozenSet<ILarkComponent> hashSet, out TComponent component) where TComponent : struct {
    var result = hashSet.FirstOrDefault(c => c.GetType() == typeof(TComponent));
    if (result is not null) {
      component = (TComponent)result;
      return true;
    }

    component = default;
    return false;
  }


  public static ValueTuple<TComponent1, TComponent2> Get<TComponent1, TComponent2>(this FrozenSet<ILarkComponent> hashSet) where TComponent1 : struct where TComponent2 : struct {
    return new ValueTuple<TComponent1, TComponent2>(
      (TComponent1)hashSet.First(c => c.GetType() == typeof(TComponent1)),
      (TComponent2)hashSet.First(c => c.GetType() == typeof(TComponent2))
    );
  }

  public static ValueTuple<TComponent1, TComponent2, TComponent3> Get<TComponent1, TComponent2, TComponent3>(this FrozenSet<ILarkComponent> hashSet) where TComponent1 : struct where TComponent2 : struct where TComponent3 : struct {
    return new ValueTuple<TComponent1, TComponent2, TComponent3>(
      (TComponent1)hashSet.First(c => c.GetType() == typeof(TComponent1)),
      (TComponent2)hashSet.First(c => c.GetType() == typeof(TComponent2)),
      (TComponent3)hashSet.First(c => c.GetType() == typeof(TComponent3))
    );
  }

  public static ValueTuple<TComponent1, TComponent2, TComponent3, TComponent4> Get<TComponent1, TComponent2, TComponent3, TComponent4>(this FrozenSet<ILarkComponent> hashSet) where TComponent1 : struct where TComponent2 : struct where TComponent3 : struct where TComponent4 : struct {
    return new ValueTuple<TComponent1, TComponent2, TComponent3, TComponent4>(
      (TComponent1)hashSet.First(c => c.GetType() == typeof(TComponent1)),
      (TComponent2)hashSet.First(c => c.GetType() == typeof(TComponent2)),
      (TComponent3)hashSet.First(c => c.GetType() == typeof(TComponent3)),
      (TComponent4)hashSet.First(c => c.GetType() == typeof(TComponent4))
    );
  }

  public static ValueTuple<TComponent1, TComponent2, TComponent3, TComponent4, TComponent5> Get<TComponent1, TComponent2, TComponent3, TComponent4, TComponent5>(this FrozenSet<ILarkComponent> hashSet) where TComponent1 : struct where TComponent2 : struct where TComponent3 : struct where TComponent4 : struct where TComponent5 : struct {
    return new ValueTuple<TComponent1, TComponent2, TComponent3, TComponent4, TComponent5>(
      (TComponent1)hashSet.First(c => c.GetType() == typeof(TComponent1)),
      (TComponent2)hashSet.First(c => c.GetType() == typeof(TComponent2)),
      (TComponent3)hashSet.First(c => c.GetType() == typeof(TComponent3)),
      (TComponent4)hashSet.First(c => c.GetType() == typeof(TComponent4)),
      (TComponent5)hashSet.First(c => c.GetType() == typeof(TComponent5))
    );
  }

  public static bool Has<TComponent>(this FrozenSet<ILarkComponent> hashSet) where TComponent : struct {
    return hashSet.Any(c => c.GetType() == typeof(TComponent));
  }
}
