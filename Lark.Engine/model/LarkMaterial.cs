using Silk.NET.Maths;

namespace Lark.Engine.model;

public class LarkMaterial {
  public Vector4D<float> BaseColorFactor = new(1, 1, 1, 1);
  public int? BaseColorTextureIndex;
}
