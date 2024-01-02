using System.Numerics;

namespace Lark.Engine.std;
public static class VectorUtils {
  public static Vector3 Berp(Vector3 start, Vector3 end, float time, ILarkCurve curve) {
    // Vector3 direction = Vector3.Normalize(end - start);
    // float distance = Vector3.Distance(start, end);
    // if (start == end) {
    //   direction = Vector3.Zero;
    //   distance = 1;
    // }
    var curvePosition = curve.ComputePoint(time);

    var distance = Vector3.Distance(start, end);
    var forward = Vector3.Normalize(end - start);

    if (start == end) {
      distance = 1;
      forward = Vector3.UnitZ;
    }

    // Create scaling part of the matrix, if start and end are the same, use identity.
    var scale = Matrix4x4.CreateScale(distance);

    // Create a rotation parts of the final matrix.
    var right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, forward));
    var up = Vector3.Cross(forward, right);

    var rotation = new Matrix4x4(
      right.X, right.Y, right.Z, 0,
      up.X, up.Y, up.Z, 0,
      forward.X, forward.Y, forward.Z, 0,
      0, 0, 0, 1
    );
    // Create a translation part of the matrix.
    var translation = Matrix4x4.CreateTranslation(start);

    // Combine all 3 matrices into one.
    var result = scale * rotation * translation;

    var rotatedPoint = Vector3.Transform(curvePosition, result);

    return rotatedPoint;
  }
}
