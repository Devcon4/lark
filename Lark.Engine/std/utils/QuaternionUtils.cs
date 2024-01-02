
using System.Numerics;

namespace Lark.Engine.std;

public static partial class LarkUtils {
  // Create LookAt

  // CreateFromYawPitchRollDegree using degrees
  public static Quaternion CreateFromYawPitchRoll(float yaw, float pitch, float roll) {
    return Quaternion.CreateFromYawPitchRoll(DegToRad(yaw), DegToRad(pitch), DegToRad(roll));
  }

  // public static Quaternion LookAt(Vector3 position, Vector3 destPoint, Vector3 upDir) {
  //   var forward = Vector3.Normalize(destPoint - position);
  //   var right = Vector3.Normalize(Vector3.Cross(upDir, forward));
  //   var up = Vector3.Cross(forward, right);

  //   var matrix = new Matrix4x4(
  //     right.X, right.Y, right.Z, 0,
  //     up.X, up.Y, up.Z, 0,
  //     forward.X, forward.Y, forward.Z, 0,
  //     0, 0, 0, 1
  //   );
  //   return Quaternion.CreateFromRotationMatrix(matrix);
  // }

  public static Quaternion LookAt(Vector3 position, Vector3 destination, Vector3 up) {
    Vector3 forward = Vector3.Normalize(destination - position);
    Vector3 right = Vector3.Normalize(Vector3.Cross(up, forward));
    Vector3 upNew = Vector3.Cross(forward, right);

    Matrix4x4 matrix = new Matrix4x4(
        right.X, right.Y, right.Z, 0,
        upNew.X, upNew.Y, upNew.Z, 0,
        -forward.X, -forward.Y, -forward.Z, 0,
        0, 0, 0, 1
    );

    return Quaternion.CreateFromRotationMatrix(matrix);
  }
}