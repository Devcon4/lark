using Silk.NET.Vulkan;

namespace Lark.Engine.pipeline;

public class ImageViewSegment(LarkVulkanData data) {
  public unsafe void CreateImageViews() {
    for (var i = 0; i < data.SwapchainImages.Length; i++) {
      var createInfo = new ImageViewCreateInfo {
        SType = StructureType.ImageViewCreateInfo,
        Image = data.SwapchainImages.Span[i].Image,
        ViewType = ImageViewType.Type2D,
        Format = data.SwapchainImageFormat,
        Components = {
            R = ComponentSwizzle.Identity,
            G = ComponentSwizzle.Identity,
            B = ComponentSwizzle.Identity,
            A = ComponentSwizzle.Identity
        },
        SubresourceRange = {
          AspectMask = ImageAspectFlags.ColorBit,
          BaseMipLevel = 0,
          LevelCount = 1,
          BaseArrayLayer = 0,
          LayerCount = 1
        }
      };

      ImageView imageView = default;
      if (data.vk.CreateImageView(data.Device, &createInfo, null, &imageView) != Result.Success) {
        throw new Exception("failed to create image views!");
      }

      data.SwapchainImages.Span[i].View = imageView;
    }
  }
}