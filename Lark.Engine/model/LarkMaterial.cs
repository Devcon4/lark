using Lark.Engine.pipeline;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace Lark.Engine.model;

public class LarkMaterial {
  public Vector4D<float> BaseColorFactor = new(1, 1, 1, 1);
  public LarkImage ORMTexture;
  public int? BaseColorTextureIndex;

  public Memory<DescriptorSet> ormDescriptorSets;
}
