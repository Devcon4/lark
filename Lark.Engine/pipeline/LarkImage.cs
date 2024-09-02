using Lark.Engine.pipeline;
using Silk.NET.Vulkan;
using Image = Silk.NET.Vulkan.Image;

namespace Lark.Engine.pipeline;

public struct LarkImage() {
  private Guid? _id = null;

  // Becuase this is a struct other methods of setting the id resulted in Guid.Empty.
  public Guid Id {
    get {
      if (_id == null) {
        _id = Guid.NewGuid();
      }

      return _id.Value;
    }
  }
  public Image Image;
  public DeviceMemory Memory;
  public ImageView View;
  public Sampler Sampler;
  public DescriptorSet[] DescriptorSets;
  public ImageLayout Layout = ImageLayout.Undefined;

  public unsafe void Dispose(LarkVulkanData data) {
    data.vk.DestroySampler(data.Device, Sampler, null);
    data.vk.DestroyImageView(data.Device, View, null);
    data.vk.DestroyImage(data.Device, Image, null);
    data.vk.FreeMemory(data.Device, Memory, null);
  }
}
