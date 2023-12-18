
namespace Lark.Engine.std;

public static partial class LarkUtils {
  public static float DegToRad(float degrees) => MathF.PI / 180f * degrees;
  public static float RadToDeg(float radians) => 180f / MathF.PI * radians;
}