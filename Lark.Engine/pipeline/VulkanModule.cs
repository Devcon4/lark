using Lark.Engine.pipeline;

namespace Lark.Engine.pipeline;

public class VulkanModule(VulkanBuilder vulkanBuilder, LarkWindow larkWindow) : ILarkModule {
  public Task Cleanup() {
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
