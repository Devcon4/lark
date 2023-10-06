using Lark.Engine.ecs;
using Lark.Engine.Pipeline;
using Microsoft.Extensions.Logging;

namespace Lark.Engine;

public partial class Engine(LarkVulkanData data, LarkWindow larkWindow, VulkanBuilder vulkanBuilder, SystemManager systemManager, ILogger<Engine> logger) {
  public async Task Run() {
    logger.LogInformation("Running engine...");
    larkWindow.Build();
    larkWindow.SetFramebufferResize(vulkanBuilder.FramebufferResize);
    systemManager.Init();

    await Init();

    await larkWindow.Run(GameLoop);
  }

  public async Task GameLoop() {
    logger.LogDebug("{frame} :: {realFrame} :: Game looping", data.CurrentFrame, data.CurrF);
    await systemManager.Run();
    vulkanBuilder.DrawFrame();
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