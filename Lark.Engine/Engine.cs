using Lark.Engine.Pipeline;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;

namespace Lark.Engine;

public partial class Engine(LarkWindow larkWindow, VulkanBuilder vulkanBuilder, ILogger<Engine> logger) {
  public async Task Run() {
    logger.LogInformation("Running engine...");
    larkWindow.Build();
    larkWindow.SetFramebufferResize(vulkanBuilder.FramebufferResize);

    await Init();

    larkWindow.Run(vulkanBuilder.DrawFrame);
  }

  private async Task Init() {
    logger.LogInformation("Initializing engine...");
    vulkanBuilder.InitVulkan();
    await Task.CompletedTask;
  }

  public async Task Cleanup() {
    logger.LogInformation("Disposing engine...");
    _ = Task.Run(() => {
      vulkanBuilder.Cleanup();
      larkWindow.Cleanup();
    });
    await Task.CompletedTask;
  }
}