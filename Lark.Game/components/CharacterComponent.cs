using System.Numerics;
using Lark.Engine.ecs;

namespace Lark.Game.components;


public record struct CharacterComponent(float Speed, Guid PlayerId, Guid BodyId) : ILarkComponent { }
public record struct CharacterRotationComponent(Vector2 LastMousePosition, float TotalPitch, Quaternion Rotation) : ILarkComponent { }
public record struct CharacterDisplacementComponent() : ILarkComponent {
  public HashSet<Func<Vector3, Vector3>> Modifiers { get; init; } = [];
  public void Set(Func<Vector3, Vector3> modifier) => this = this with { Modifiers = [.. Modifiers, modifier] };
}