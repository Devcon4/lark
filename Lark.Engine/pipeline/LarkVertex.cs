using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Lark.Engine.pipeline;

public struct LarkVertex(Vector3D<float> pos, Vector3D<float> normal, Vector2D<float> uv, Vector3D<float> color) {
  public Vector3D<float> Pos = pos;
  public Vector3D<float> Normal = normal;
  public Vector2D<float> UV = uv;
  public Vector3D<float> Color = color;

  public static VertexInputBindingDescription GetBindingDescription() {
    return new() {
      Binding = 0,
      Stride = (uint)Marshal.SizeOf<LarkVertex>(),
      InputRate = VertexInputRate.Vertex
    };
  }

  public static VertexInputAttributeDescription[] GetAttributeDescriptions() {
    return new[] {
      new VertexInputAttributeDescription {
        Binding = 0,
        Location = 0,
        Format = Format.R32G32B32Sfloat,
        Offset = (uint)Marshal.OffsetOf<LarkVertex>(nameof(Pos))
      },
      new VertexInputAttributeDescription {
        Binding = 0,
        Location = 1,
        Format = Format.R32G32B32Sfloat,
        Offset = (uint)Marshal.OffsetOf<LarkVertex>(nameof(Color))
      },
      new VertexInputAttributeDescription {
        Binding = 0,
        Location = 2,
        Format = Format.R32G32B32Sfloat,
        Offset = (uint)Marshal.OffsetOf<LarkVertex>(nameof(UV))
      },
      new VertexInputAttributeDescription {
        Binding = 0,
        Location = 3,
        Format = Format.R32G32B32Sfloat,
        Offset = (uint)Marshal.OffsetOf<LarkVertex>(nameof(Normal))
      }
    };
  }
}
