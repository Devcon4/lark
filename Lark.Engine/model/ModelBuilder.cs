using System.Numerics;
using Lark.Engine.Pipeline;
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
  public bool Active;
  public LarkTransform Transform;
  public float Fov;
  public float Near;
  public float Far;
  public float AspectRatio;

  public LarkCamera(LarkTransform transform, float fov, float near, float far, float aspectRatio, bool active = false) {
    Transform = transform;
    Fov = fov;
    Near = near;
    Far = far;
    AspectRatio = aspectRatio;
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
    16 / 9f
  );

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