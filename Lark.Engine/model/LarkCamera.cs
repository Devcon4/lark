using System.Numerics;
using Silk.NET.Maths;

namespace Lark.Engine.model;

// LarkCamera
public struct LarkCamera {
  public bool Active = false;
  public LarkTransform Transform = new();
  public float Fov = 90f;
  public float Near = 0.1f;
  public float Far = 1000f;
  public float AspectRatio = 16f / 9f;

  public Vector2 ViewportSize = new(1080, 720);

  public LarkCamera(LarkTransform transform, float fov, float near, float far, float aspectRatio, Vector2 viewportSize, bool active = false) {
    Transform = transform;
    Fov = fov;
    Near = near;
    Far = far;
    AspectRatio = aspectRatio;
    ViewportSize = viewportSize;
    Active = active;
  }

  public static LarkCamera DefaultCamera() => new LarkCamera(
    new(
      new Vector3D<float>(0, -1, 3),
      Quaternion<float>.CreateFromAxisAngle(new Vector3D<float>(0, -1, 0), 0),
      new Vector3D<float>(1, 1, 1)),
    100f,
    0.1f,
    100f,
    16 / 9f,
    new Vector2(1080, 720)
  );

  public readonly Matrix4x4 View {
    get {
      var view = Matrix4x4.CreateTranslation(-Transform.Translation.ToSystem()) * Matrix4x4.CreateFromQuaternion(Transform.Rotation.ToSystem());
      return view;
    }
  }
  public readonly Matrix4x4 Projection {
    get {
      var proj = Matrix4x4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(Fov), AspectRatio, Near, Far);
      // proj.M22 *= -1;
      return proj;
    }
  }

  public readonly Matrix4x4 InvertView {
    get {
      Matrix4x4.Invert(View, out var invertView);
      return invertView;
    }
  }

  public readonly Matrix4x4 InvertProjection {
    get {
      Matrix4x4.Invert(Projection, out var invertProjection);
      return invertProjection;
    }
  }

  // Matrix translating from [-1,-1]:[1,1] to [0,0]:[ViewportSize]; he order of operations of the matrix is to scale by half the screen size, then to translate by half the screen size.
  public Matrix4x4 ViewToScreen => Matrix4x4.CreateScale(0.5f * ViewportSize.X, 0.5f * ViewportSize.Y, 1) * Matrix4x4.CreateTranslation(0.5f * ViewportSize.X, 0.5f * ViewportSize.Y, 0);
  public Matrix4x4 ScreenToView => Matrix4x4.CreateTranslation(-0.5f * ViewportSize.X, -0.5f * ViewportSize.Y, 0) * Matrix4x4.CreateScale(2f / ViewportSize.X, 2f / ViewportSize.Y, 1);

  public Matrix4x4 ScreenToWorld => ScreenToView * InvertView * InvertProjection * Transform.ToInverseMatrix().ToSystem();
  public Matrix4x4 WorldToScreen => Transform.ToMatrix().ToSystem() * Projection * View * ViewToScreen;

  public Vector3 ProjectToOld(Vector2 screenPosition, float zDepth) {
    var p = new Vector4(screenPosition.X, screenPosition.Y, zDepth, 1);
    var matrix = ScreenToView * InvertView * InvertProjection * Transform.ToInverseMatrix().ToSystem();
    var result = Vector4.Transform(p, matrix);
    result /= result.W;
    var final = new Vector3(-result.X, -result.Y, -result.Z);
    return final;
  }

  public Vector3 ProjectTo(Vector2 screenPosition, float zDepth) {

    var p = new Vector4(screenPosition.X, screenPosition.Y, zDepth, 1);
    var ndcPos = Vector4.Transform(p, ScreenToView);
    var cameraPos = Vector4.Transform(ndcPos, InvertProjection);
    var worldPos = Vector4.Transform(cameraPos, InvertView);
    var objectPos = Vector4.Transform(worldPos, Transform.ToMatrix().ToSystem());
    objectPos /= objectPos.W;
    return new Vector3(objectPos.X, objectPos.Y, objectPos.Z);
    // var matrix = ScreenToView * InvertView * InvertProjection * Transform.ToInverseMatrix().ToSystem();
    // var result = Vector4.Transform(p, matrix);
    // result /= result.W;
    // var final = new Vector3(-result.X, -result.Y, -result.Z);
    // return final;
  }

  public Vector3 ProjectToNear(Vector2 screenPosition) => ProjectTo(screenPosition, Near);
  public Vector3 ProjectToFar(Vector2 screenPosition) => ProjectTo(screenPosition, Far);

  public void SetAspectRatio(float aspectRatio) {
    AspectRatio = aspectRatio;
  }

  public void SetFov(float fov) {
    Fov = fov;
  }

  public void SetPosition(Vector3D<float> position) {
    Transform.Translation = position;
  }

  public void SetRotation(Vector3D<float> axis, float angle) {
    Transform.Rotation = Quaternion<float>.CreateFromAxisAngle(axis, Scalar.DegreesToRadians(angle));
  }
}
