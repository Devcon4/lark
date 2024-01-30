using System.Numerics;
using Lark.Engine.pipeline;
using Lark.Engine.std;
using SharpGLTF.Transforms;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
using Image = Silk.NET.Vulkan.Image;

namespace Lark.Engine.Model;

public struct LarkPrimitive {
  public int FirstIndex;
  public int IndexCount;
  public int MaterialIndex;
}

public struct LarkImage {
  public Image Image;
  public DeviceMemory Memory;
  public ImageView View;
  public Sampler Sampler;
  public DescriptorSet[] DescriptorSets;

  public unsafe void Dispose(LarkVulkanData data) {
    data.vk.DestroySampler(data.Device, Sampler, null);
    data.vk.DestroyImageView(data.Device, View, null);
    data.vk.DestroyImage(data.Device, Image, null);
    data.vk.FreeMemory(data.Device, Memory, null);
  }
}

public struct LarkTexture {
  public int TextureIndex;
  public int? SamplerIndex;
}

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
      var translation = Matrix4x4.CreateTranslation(-Transform.Translation.ToSystem());
      var rotation = Matrix4x4.Transpose(Matrix4x4.CreateFromQuaternion(Transform.Rotation.ToSystem()));
      return rotation * translation;
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

  public Vector3 ProjectTo(Vector2 screenPosition, float zDepth) {
    var p = new Vector4(screenPosition.X, screenPosition.Y, 1, zDepth);
    var matrix = ScreenToView * InvertView * InvertProjection * Transform.ToInverseMatrix().ToSystem();
    var result = Vector4.Transform(p, matrix);
    result /= result.W;
    var final = new Vector3(-result.X, -result.Y, -result.Z);
    return final;
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

public struct LarkTransform {
  public Vector3D<float> Translation = new(0, 0, 0);
  public Quaternion<float> Rotation = Quaternion<float>.Identity;
  public Vector3D<float> Scale = new(1, 1, 1);

  public void RotateByAxisAndAngle(Vector3D<float> axis, float angle) => Rotation *= Quaternion<float>.CreateFromAxisAngle(axis, angle);

  public LarkTransform() { }
  public LarkTransform(Vector3D<float> translation, Quaternion<float> rotation, Vector3D<float> scale) {
    Translation = translation;
    Rotation = rotation;
    Scale = scale;
  }
  public LarkTransform(Matrix4x4 matrix) {
    Matrix4x4.Decompose(matrix, out var scale, out var rotation, out var translation);
    Translation = translation.ToGeneric();
    Rotation = rotation.ToGeneric();
    Scale = scale.ToGeneric();
  }
  public LarkTransform(AffineTransform transform) {
    if (transform.IsMatrix) {
      this = new(transform.Matrix);
      return;
    }

    Translation = transform.Translation.ToGeneric();
    Rotation = transform.Rotation.ToGeneric();
    Scale = transform.Scale.ToGeneric();
  }

  public LarkTransform(TransformComponent transform) {
    Translation = transform.Position.ToGeneric();
    Rotation = transform.Rotation.ToGeneric();
    Scale = transform.Scale.ToGeneric();
  }

  // Add a multiply operator for LarkTransforms. It multiplys, rotation, and scale but adds the translation of the two transforms.
  public static LarkTransform operator *(LarkTransform a, LarkTransform b) {
    return new() {
      Translation = a.Translation + b.Translation,
      Rotation = a.Rotation * b.Rotation,
      Scale = a.Scale * b.Scale
    };
  }

  // ToMatrix4x4: Convert the LarkTransform to a Matrix4x4.
  public readonly Matrix4X4<float> ToMatrix() {
    return (Matrix4x4.CreateScale((Vector3)Scale) * Matrix4x4.CreateFromQuaternion((Quaternion)Rotation) * Matrix4x4.CreateTranslation((Vector3)Translation)).ToGeneric();
  }

  public readonly Matrix4X4<float> ToInverseMatrix() {
    Matrix4X4.Invert(ToMatrix(), out var inverse);
    return inverse;
  }
}

public record struct LarkInstance() {
  public Guid InstanceId = Guid.NewGuid();
  public LarkTransform Transform;
  public Guid ModelId;
}

public struct LarkNode {
  public unsafe LarkNode[] Children;
  public LarkPrimitive[] Primitives;
  public LarkTransform Transform;
}

public class LarkModel {
  public Guid ModelId = Guid.NewGuid();
  public LarkTransform Transform = new();
  public LarkBuffer Vertices;
  public LarkBuffer Indices;
  public Memory<LarkImage> Images;
  public Memory<LarkTexture> Textures;
  public Memory<LarkMaterial> Materials;
  public Memory<LarkNode> Nodes;
  public List<Vertex> meshVertices = new();
  public List<ushort> meshIndices = new();
  public DescriptorPool DescriptorPool;

  public DescriptorSet MatrixDescriptorSet;

  public int IndiceOffset = 0;

  public unsafe void Dispose(LarkVulkanData data) {
    data.vk.DestroyDescriptorPool(data.Device, DescriptorPool, null);

    data.vk.DestroyBuffer(data.Device, Vertices.Buffer, null);
    data.vk.FreeMemory(data.Device, Vertices.Memory, null);

    data.vk.DestroyBuffer(data.Device, Indices.Buffer, null);
    data.vk.FreeMemory(data.Device, Indices.Memory, null);

    Vertices.Dispose(data);
    Indices.Dispose(data);

    foreach (var image in Images.Span) {
      image.Dispose(data);
    }
  }
}

public class LarkMaterial {
  public Vector4D<float> BaseColorFactor = new(1, 1, 1, 1);
  public int? BaseColorTextureIndex;
}

public class ModelBuilder(LarkVulkanData data, ModelUtils modelUtils) {
}