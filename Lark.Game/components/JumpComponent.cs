using System.Numerics;
using Lark.Engine.ecs;

namespace Lark.Game.components;
public record struct JumpComponent(Vector3 Start, Vector3 End, TimeSpan Duration) : ILarkComponent {
  public float Progress { get; set; }
  public Vector3 JumpPosition { get; set; }
  public Vector3 VelocityDelta { get; set; }
}
