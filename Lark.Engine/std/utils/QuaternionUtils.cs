
using System.Numerics;

namespace Lark.Engine.std;

public static partial class LarkUtils {
  // Create LookAt

  // CreateFromYawPitchRollDegree using degrees
  public static Quaternion CreateFromYawPitchRoll(float yaw, float pitch, float roll) {
    return Quaternion.CreateFromYawPitchRoll(DegToRad(yaw), DegToRad(pitch), DegToRad(roll));
  }
}