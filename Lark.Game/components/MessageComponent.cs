using Lark.Engine.ecs;

namespace Lark.Game.components;

public struct MessageComponent : ILarkComponent {
  public string text;
}

public struct PositionComponent : ILarkComponent {
  public int x;
  public int y;
}
