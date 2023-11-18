using System.Numerics;
using Lark.Engine.ecs;

namespace Lark.Engine.std;

public record struct TransformComponent(Vector3 Position, Vector3 Scale, Quaternion Rotation) : ILarkComponent {
  public TransformComponent() : this(Vector3.Zero, Vector3.One, Quaternion.Identity) { }
}
