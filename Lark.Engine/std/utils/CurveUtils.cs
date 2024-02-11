
using System.Numerics;

namespace Lark.Engine.std;

public interface ILarkCurve {
  Vector3 ComputePoint(float t);
}

public record LarkCubicBezier(Vector3 P0, Vector3 P1, Vector3 P2, Vector3 P3) : ILarkCurve {
  public Vector3 ComputePoint(float t) {
    float u = 1 - t;
    float tt = t * t;
    float uu = u * u;
    float uuu = uu * u;
    float ttt = tt * t;

    var B = uuu * P0; // (1-t)^3 * P0
    B += 3 * uu * t * P1; // 3*(1-t)^2 * t * P1
    B += 3 * u * tt * P2; // 3*(1-t) * t^2 * P2
    B += ttt * P3; // t^3 * P3

    return B;
  }
}
public class LarkSlerp(Vector3 start, Vector3 end) : ILarkCurve {
  public Vector3 ComputePoint(float t) {
    // Compute the cosine of the angle between the two vectors.
    var cosTheta = Vector3.Dot(start, end);

    // Clamp the value to the range [-1, 1] to account for possible floating-point errors
    cosTheta = Math.Clamp(cosTheta, -1f, 1f);

    // Compute the angle between the start and end vectors
    var theta = Math.Acos(cosTheta);

    // If the angle is close to zero, just linearly interpolate
    if (Math.Abs(theta) < 1e-5) {
      return Vector3.Lerp(start, end, t);
    }

    // Perform the Slerp
    var sinTheta = Math.Sin(theta);
    return (float)(Math.Sin((1 - t) * theta) / sinTheta) * start + (float)(Math.Sin(t * theta) / sinTheta) * end;
  }
}

public class LarkBezier : ILarkCurve {
  public Vector3[] ControlPoints { get; }

  public LarkBezier(params Vector3[] controlPoints) {
    ControlPoints = controlPoints;
  }

  public Vector3 ComputePoint(float t) {
    Vector3[] points = new Vector3[ControlPoints.Length];
    ControlPoints.CopyTo(points, 0);

    int i, j;
    for (i = 1; i < ControlPoints.Length; i++) {
      for (j = 0; j < ControlPoints.Length - i; j++) {
        points[j] = (1 - t) * points[j] + t * points[j + 1];
      }
    }

    // When t is 0 or greater than 1, the result is the first or last control point, return it directly to avoid floating point errors
    if (t == 0) {
      Console.WriteLine("t == 0");
      return ControlPoints[0];
    }

    if (t >= 1) {
      return ControlPoints[^1];
    }

    return points[0];
  }
}
public static class CurveUtils {
  public static ILarkCurve Linear => new LarkCubicBezier(
    new(),
    new(),
    new(1, 1, 1),
    new(1, 1, 1)
  );

  public static ILarkCurve LinearJump => new LarkCubicBezier(
    new(),
    new(0, -1, 0),
    new(0, -1, 0),
    new()
  );

  public static ILarkCurve EaseIn => new LarkCubicBezier(
    new(),
    new(0.42f, 0, 0),
    new(1, 1, 1),
    new(1, 1, 1)
  );

  public static ILarkCurve EaseOut => new LarkCubicBezier(
    new(),
    new(),
    new(0.58f, 1, 0),
    new(1, 1, 1)
  );

  public static ILarkCurve EaseInOut => new LarkCubicBezier(
    new(),
    new(0.42f, 0, 0),
    new(0.58f, 1, 0),
    new(1, 1, 1)
  );

  // Slerp; Curve that represents a Spherical Linear Interpolation.
  // Spherical linear represents a point along a circle that touches the start and end points.
  public static ILarkCurve Slerp => new LarkSlerp(
    new(0, 0, 0),
    new(1, 1, 1)
  );

  // SlertXZ; Slerp but only in the XZ plane
  public static ILarkCurve SlerpXZ => new LarkSlerp(
    new(0, 0, 0),
    new(1, 0, 1)
  );


  public static ILarkCurve Jump => new LarkBezier(
    new(0, 0, 0), // Start at the ground
    new(0, -2, 0), // Jump up
    new(0, -2, 0), // Jump up
    new(0, -2, 0), // Jump up
    new(0, -1.6f, 0), // Jump up
    new(0, -.2f, 0), // Jump down
    new(0, .2f, 0), // Jump down
    new(0, 0, 0) // Return to the ground
  );
}