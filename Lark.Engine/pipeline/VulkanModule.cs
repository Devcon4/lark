using Lark.Engine.pipeline;
using Microsoft.Extensions.Logging;

namespace Lark.Engine.pipeline;

public class VulkanModule(VulkanBuilder vulkanBuilder, LarkWindow larkWindow, ILogger<VulkanModule> logger) : ILarkModule {
  public Task Cleanup() {
    logger.LogInformation("Cleaning up Vulkan... {thread}", Environment.CurrentManagedThreadId);
    vulkanBuilder.Cleanup();
    return Task.CompletedTask;
  }

  public Task Init() {
    larkWindow.SetFramebufferResize(vulkanBuilder.FramebufferResize);
    vulkanBuilder.InitVulkan();
    return Task.CompletedTask;
  }

  public Task Run() {
    vulkanBuilder.DrawFrame();
    return Task.CompletedTask;
  }
}
