using System.Numerics;
using Lark.Engine.std;
using SharpGLTF.Transforms;
using Silk.NET.Maths;

namespace Lark.Engine.Model;

public struct LarkTransform
{
  public Vector3D<float> Translation = new(0, 0, 0);
  public Quaternion<float> Rotation = Quaternion<float>.Identity;
  public Vector3D<float> Scale = new(1, 1, 1);

  public void RotateByAxisAndAngle(Vector3D<float> axis, float angle) => Rotation *= Quaternion<float>.CreateFromAxisAngle(axis, angle);

  public LarkTransform() { }
  public LarkTransform(Vector3D<float> translation, Quaternion<float> rotation, Vector3D<float> scale)
  {
    Translation = translation;
    Rotation = rotation;
    Scale = scale;
  }
  public LarkTransform(Matrix4x4 matrix)
  {
    Matrix4x4.Decompose(matrix, out var scale, out var rotation, out var translation);
    Translation = translation.ToGeneric();
    Rotation = rotation.ToGeneric();
    Scale = scale.ToGeneric();
  }
  public LarkTransform(AffineTransform transform)
  {
    if (transform.IsMatrix)
    {
      this = new(transform.Matrix);
      return;
    }

    Translation = transform.Translation.ToGeneric();
    Rotation = transform.Rotation.ToGeneric();
    Scale = transform.Scale.ToGeneric();
  }

  public LarkTransform(TransformComponent transform)
  {
    Translation = transform.Position.ToGeneric();
    Rotation = transform.Rotation.ToGeneric();
    Scale = transform.Scale.ToGeneric();
  }

  // Add a multiply operator for LarkTransforms. It multiplys, rotation, and scale but adds the translation of the two transforms.
  public static LarkTransform operator *(LarkTransform a, LarkTransform b)
  {
    return new()
    {
      Translation = a.Translation + b.Translation,
      Rotation = a.Rotation * b.Rotation,
      Scale = a.Scale * b.Scale
    };
  }

  // ToMatrix4x4: Convert the LarkTransform to a Matrix4x4.
  public readonly Matrix4X4<float> ToMatrix()
  {
    return (Matrix4x4.CreateScale((Vector3)Scale) * Matrix4x4.CreateFromQuaternion((Quaternion)Rotation) * Matrix4x4.CreateTranslation((Vector3)Translation)).ToGeneric();
  }

  public readonly Matrix4X4<float> ToInverseMatrix()
  {
    Matrix4X4.Invert(ToMatrix(), out var inverse);
    return inverse;
  }
}
