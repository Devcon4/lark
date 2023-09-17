using Silk.NET.Vulkan;

namespace Lark.Engine.Pipeline;

public class SurfaceSegment(LarkVulkanData data, LarkWindow larkWindow) {
  public unsafe void CreateSurface() {
    data.Surface = larkWindow.rawWindow.VkSurface!.Create<AllocationCallbacks>(data.Instance.ToHandle(), null).ToSurface();
  }
}