
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

  // For a given direction vector, return the quaternion representing the rotation
  public static Quaternion Facing(Vector3 direction) {
    var forward = Vector3.Normalize(direction);
    var right = Vector3.Normalize(Vector3.Cross(-Vector3.UnitY, forward));

    if (forward == Vector3.UnitY || forward == -Vector3.UnitY) {
      right = Vector3.UnitX;
    }

    var up = Vector3.Cross(forward, right);

    var matrix = new Matrix4x4(
      right.X, right.Y, right.Z, 0,
      up.X, up.Y, up.Z, 0,
      forward.X, forward.Y, forward.Z, 0,
      0, 0, 0, 1
    );
    return Quaternion.CreateFromRotationMatrix(matrix);
  }

  public static Quaternion RotationFromNormal(Vector3 normal) {
    // Create an orthonormal basis from the normal
    Vector3 up = -Vector3.UnitY;
    Vector3 right = Vector3.Normalize(Vector3.Cross(up, normal));
    up = Vector3.Cross(normal, right);

    // Create a rotation matrix from the orthonormal basis
    Matrix4x4 matrix = new Matrix4x4(
      right.X, right.Y, right.Z, 0,
      up.X, up.Y, up.Z, 0,
      normal.X, normal.Y, normal.Z, 0,
      0, 0, 0, 1);

    // Convert the rotation matrix to a quaternion
    return Quaternion.CreateFromRotationMatrix(matrix);
  }
}
