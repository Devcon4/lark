using Lark.Engine.Pipeline;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;

namespace Lark.Engine;

public partial class Engine(LarkWindow larkWindow, VulkanBuilder vulkanBuilder, ILogger<Engine> logger) {
  public async Task Run() {
    logger.LogInformation("Running engine...");
    larkWindow.Build(window => {
      window.Load += OnLoad;
      window.Update += OnUpdate;
      window.Render += vulkanBuilder.DrawFrame;
    });

    await Init();

    _ = Task.Run(() => {
      larkWindow.Run();
      vulkanBuilder.Wait();
    });
  }

  private async Task Init() {
    logger.LogInformation("Initializing engine...");
    logger.LogInformation("Window: {Window}", larkWindow.rawWindow);
    vulkanBuilder.InitVulkan();
    await Task.CompletedTask;
  }

  private void OnUpdate(double obj) {
    logger.LogDebug("Update");
  }

  private void OnLoad() {
    logger.LogInformation("Window loaded.");
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