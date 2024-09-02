using Lark.Engine.pipeline;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Lark.Engine.model;

public struct LarkPrimitive {
  public int FirstIndex;
  public int IndexCount;
  public int MaterialIndex;
}

public struct LarkTexture {
  public int TextureIndex;
  public int? SamplerIndex;
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
  public List<LarkVertex> meshVertices = new();
  public List<ushort> meshIndices = new();
  public DescriptorPool DescriptorPool;

  public Memory<DescriptorSet> MatrixDescriptorSet = new DescriptorSet[LarkVulkanData.MaxFramesInFlight];

  public int IndiceOffset = 0;

  public unsafe void Dispose(LarkVulkanData data) {
    data.vk.DestroyDescriptorPool(data.Device, DescriptorPool, null);

    Vertices.Dispose(data);
    Indices.Dispose(data);

    foreach (var image in Images.Span) {
      image.Dispose(data);
    }
  }
}
