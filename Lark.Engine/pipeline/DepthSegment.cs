using Microsoft.Extensions.Logging;
using Silk.NET.Vulkan;

namespace Lark.Engine.Pipeline;

public class DepthSegment(LarkVulkanData data, ImageUtils imageUtils, ILogger<DepthSegment> logger) {

  public void CreateDepthResources() {
    var depthFormat = imageUtils.FindDepthFormat();

    imageUtils.CreateImage(data.SwapchainExtent.Width, data.SwapchainExtent.Height, depthFormat, ImageTiling.Optimal, ImageUsageFlags.DepthStencilAttachmentBit, MemoryPropertyFlags.DeviceLocalBit, ref data.DepthImage, ref data.DepthImageMemory);
    data.DepthImageView = imageUtils.CreateImageView(data.DepthImage, depthFormat, ImageAspectFlags.DepthBit);
  }
}