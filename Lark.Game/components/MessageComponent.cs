using Lark.Engine.ecs;

namespace Lark.Game.components;

public record struct MessageComponent : ILarkComponent {
  public string text;
  public TimeSpan time;
}
