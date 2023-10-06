namespace Lark.Engine.ecs;

public static class HashSetExtensions {
  public static TComponent Get<TComponent>(this HashSet<ILarkComponent> hashSet) where TComponent : struct {
    return (TComponent)hashSet.First(c => c.GetType() == typeof(TComponent));
  }

  public static ValueTuple<TComponent1, TComponent2> Get<TComponent1, TComponent2>(this HashSet<ILarkComponent> hashSet) where TComponent1 : struct where TComponent2 : struct {
    return new ValueTuple<TComponent1, TComponent2>(
      (TComponent1)hashSet.First(c => c.GetType() == typeof(TComponent1)),
      (TComponent2)hashSet.First(c => c.GetType() == typeof(TComponent2))
    );
  }

  public static ValueTuple<TComponent1, TComponent2, TComponent3> Get<TComponent1, TComponent2, TComponent3>(this HashSet<ILarkComponent> hashSet) where TComponent1 : struct where TComponent2 : struct where TComponent3 : struct {
    return new ValueTuple<TComponent1, TComponent2, TComponent3>(
      (TComponent1)hashSet.First(c => c.GetType() == typeof(TComponent1)),
      (TComponent2)hashSet.First(c => c.GetType() == typeof(TComponent2)),
      (TComponent3)hashSet.First(c => c.GetType() == typeof(TComponent3))
    );
  }

  public static ValueTuple<TComponent1, TComponent2, TComponent3, TComponent4> Get<TComponent1, TComponent2, TComponent3, TComponent4>(this HashSet<ILarkComponent> hashSet) where TComponent1 : struct where TComponent2 : struct where TComponent3 : struct where TComponent4 : struct {
    return new ValueTuple<TComponent1, TComponent2, TComponent3, TComponent4>(
      (TComponent1)hashSet.First(c => c.GetType() == typeof(TComponent1)),
      (TComponent2)hashSet.First(c => c.GetType() == typeof(TComponent2)),
      (TComponent3)hashSet.First(c => c.GetType() == typeof(TComponent3)),
      (TComponent4)hashSet.First(c => c.GetType() == typeof(TComponent4))
    );
  }

  public static ValueTuple<TComponent1, TComponent2, TComponent3, TComponent4, TComponent5> Get<TComponent1, TComponent2, TComponent3, TComponent4, TComponent5>(this HashSet<ILarkComponent> hashSet) where TComponent1 : struct where TComponent2 : struct where TComponent3 : struct where TComponent4 : struct where TComponent5 : struct {
    return new ValueTuple<TComponent1, TComponent2, TComponent3, TComponent4, TComponent5>(
      (TComponent1)hashSet.First(c => c.GetType() == typeof(TComponent1)),
      (TComponent2)hashSet.First(c => c.GetType() == typeof(TComponent2)),
      (TComponent3)hashSet.First(c => c.GetType() == typeof(TComponent3)),
      (TComponent4)hashSet.First(c => c.GetType() == typeof(TComponent4)),
      (TComponent5)hashSet.First(c => c.GetType() == typeof(TComponent5))
    );
  }

}
