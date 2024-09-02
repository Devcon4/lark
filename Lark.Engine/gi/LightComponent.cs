using System.Numerics;
using Lark.Engine.ecs;
using Lark.Engine.model;

namespace Lark.Engine.gi;

public record struct PointLight(LarkColor Color, float Intensity, float Range) : ILarkLight {
  public readonly Vector4 OptionsVector => new(Intensity, Range, 0, 0);
}
public record struct SpotLight(LarkColor Color, float Intensity, float Range, float Angle) : ILarkLight {
  public readonly Vector4 OptionsVector => new(Intensity, Range, Angle, 0);
}
public record struct DirectionalLight(LarkColor Color, float Intensity) : ILarkLight {
  public readonly Vector4 OptionsVector => new(Intensity, 0, 0, 0);
}

public record struct LightComponent(ILarkLight Settings) : ILarkComponent { }
// public record struct LightInstanceComponent(ILarkLight Settings) : ILarkComponent { }

// public record struct SpotLightComponent(LarkColor Color, float Intensity, float Range, float Angle) : ILarkComponent { }
// public record struct DirectionalLightComponent(LarkColor Color, float Intensity) : ILarkComponent { }
public record struct ProbeGroupComponent(Vector3 Rect, float Density) : ILarkComponent { }