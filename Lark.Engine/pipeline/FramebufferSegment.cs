using Microsoft.Extensions.Logging;
using Silk.NET.Vulkan;

namespace Lark.Engine.Pipeline;

public class FramebufferSegment(LarkVulkanData data, ILogger<FramebufferSegment> logger) {
  public unsafe void CreateFramebuffers() {
    data.SwapchainFramebuffers = new Framebuffer[data.SwapchainImageViews.Length];

    for (var i = 0; i < data.SwapchainImageViews.Length; i++) {
      var attachment = data.SwapchainImageViews[i];
      var framebufferInfo = new FramebufferCreateInfo {
        SType = StructureType.FramebufferCreateInfo,
        RenderPass = data.RenderPass,
        AttachmentCount = 1,
        PAttachments = &attachment,
        Width = data.SwapchainExtent.Width,
        Height = data.SwapchainExtent.Height,
        Layers = 1
      };

      var framebuffer = new Framebuffer();
      if (data.vk.CreateFramebuffer(data.Device, &framebufferInfo, null, &framebuffer) != Result.Success) {
        throw new Exception("failed to create framebuffer!");
      }

      data.SwapchainFramebuffers[i] = framebuffer;
    }

    logger.LogInformation("Created framebuffers.");
  }
}