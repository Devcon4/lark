using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Lark.Engine.Pipeline;

public class SurfaceSegment(LarkVulkanData data, LarkWindow larkWindow) {
  public unsafe void CreateSurface() {
    larkWindow.CreateVkSurface(data.Instance.ToHandle(), out var surfaceHandler);
    data.Surface = surfaceHandler.ToSurface();
  }
}