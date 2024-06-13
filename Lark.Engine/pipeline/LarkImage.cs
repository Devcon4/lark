using Lark.Engine.pipeline;
using Silk.NET.Vulkan;
using Image = Silk.NET.Vulkan.Image;

namespace Lark.Engine.pipeline;

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
