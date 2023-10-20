using Lark.Engine.ecs;
using Lark.Engine.gui;
using Lark.Engine.Pipeline;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;

namespace Lark.Engine;

public partial class Engine(
  LarkVulkanData data,
  LarkWindow larkWindow,
  VulkanBuilder vulkanBuilder,
  SystemManager systemManager,
  GuiManager guiManager,
  ILogger<Engine> logger) {
  public async Task Run() {
    logger.LogInformation("Running engine...");
    larkWindow.Build();
    larkWindow.SetFramebufferResize(resize);

    systemManager.Init();
    await guiManager.Init();
    vulkanBuilder.InitVulkan();

    await larkWindow.Run(GameLoop);
  }

  private void resize(Vector2D<int> size) {
    vulkanBuilder.FramebufferResize(size);
    guiManager.Resize(size);
  }

  public async Task GameLoop() {
    logger.LogDebug("{frame} :: {realFrame} :: Game looping", data.CurrentFrame, data.CurrF);
    await systemManager.Run();
    vulkanBuilder.DrawFrame();
  }

  public async Task Cleanup() {
    logger.LogInformation("Disposing engine...");
    _ = Task.Run(() => {
      guiManager.Cleanup();
      vulkanBuilder.Cleanup();
      larkWindow.Cleanup();
    });
    await Task.CompletedTask;
  }
}