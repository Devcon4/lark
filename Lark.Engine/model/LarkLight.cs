using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Lark.Engine.model;

public record LarkColor(float R, float G, float B, float A = 1.0f) { }
public interface ILarkLight {
  LarkColor Color { get; set; }
  Vector4 OptionsVector { get; }
}

public class LarkLight {
  public Guid LightId = Guid.NewGuid();
  public required ILarkLight Settings { get; set; }
  public required LarkTransform Transform { get; set; }

  public LarkLightShader ToShader() => new() {
    Color = new Vector4(Settings.Color.R, Settings.Color.G, Settings.Color.B, Settings.Color.A),
    Position = new Vector4(Transform.Translation.X, Transform.Translation.Y, Transform.Translation.Z, 1.0f),
    Options = Settings.OptionsVector
  };
}

[StructLayout(LayoutKind.Sequential)]
public struct LarkLightShader {
  public Vector4 Color;
  public Vector4 Position;
  public Vector4 Options; // x = Intensity, y = Range, z = Angle, w = unused/padding
}
