using Lark.Engine.ecs;
using Lark.Engine.physx.managers;
using Lark.Engine.pipeline;
using Microsoft.Extensions.Logging;

namespace Lark.Engine;

public class EcsModule(SystemManager systemManager) : ILarkModule {
  public Task Cleanup() {
    return Task.CompletedTask;
  }

  public Task Init() {
    systemManager.Init();
    return Task.CompletedTask;
  }

  public async Task Run() {
    await systemManager.Run();
  }
}

public partial class Engine(LarkWindow larkWindow, IEnumerable<ILarkModule> modules, ILogger<Engine> logger) {
  public async Task Run() {
    logger.LogInformation("Running engine...");
    larkWindow.Build();

    await Init();
    await larkWindow.Run(GameLoop);
  }

  public async Task GameLoop() {
    foreach (var module in modules) {
      await module.Run();
    }
  }

  private async Task Init() {
    logger.LogInformation("Initializing engine...");

    foreach (var module in modules) {
      logger.LogInformation("Initializing module {module}", module.GetType().Name);
      await module.Init();
    }

    await Task.CompletedTask;
  }

  public async Task Cleanup() {
    logger.LogInformation("Disposing engine...");
    _ = Task.Run(() => {

      foreach (var module in modules) {
        logger.LogInformation("Disposing module {module}", module.GetType().Name);
        module.Cleanup();
      }

      larkWindow.Cleanup();
    });
    await Task.CompletedTask;
  }
}