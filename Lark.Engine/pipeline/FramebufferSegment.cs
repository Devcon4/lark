using Microsoft.Extensions.Logging;
using Silk.NET.Vulkan;

namespace Lark.Engine.pipeline;

public class FramebufferSegment(LarkVulkanData data, ILogger<FramebufferSegment> logger) {
  public unsafe void CreateFramebuffers() {
    data.SwapchainFramebuffers = new Framebuffer[data.SwapchainImageViews.Length];

    for (var i = 0; i < data.SwapchainImageViews.Length; i++) {
      var attachments = new[] { data.SwapchainImageViews[i], data.DepthImageView };

      fixed (ImageView* attachmentsPtr = attachments) {
        var framebufferInfo = new FramebufferCreateInfo {
          SType = StructureType.FramebufferCreateInfo,
          RenderPass = data.RenderPass,
          AttachmentCount = (uint)attachments.Length,
          PAttachments = attachmentsPtr,
          Width = data.SwapchainExtent.Width,
          Height = data.SwapchainExtent.Height,
          Layers = 1
        };

        if (data.vk.CreateFramebuffer(data.Device, &framebufferInfo, null, out data.SwapchainFramebuffers[i]) != Result.Success) {
          throw new Exception("failed to create framebuffer!");
        }
      }
    }

    logger.LogInformation("Created framebuffers.");
  }
}