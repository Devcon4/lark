using Lark.Engine.ecs;

namespace Lark.Engine.std;

public record struct CameraComponent(
  bool Active,
  float Near,
  float Far,
  float Fov,
  float AspectRatio,
  bool FixedAspectRatio
) : ILarkComponent {
  public CameraComponent() : this(false, 0.1f, 1000f, 90f, 16f / 9f, false) { }
}
