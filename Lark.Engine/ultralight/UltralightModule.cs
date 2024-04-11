

namespace Lark.Engine.Ultralight;

public class UltralightModule(UltralightController ultralightController) : ILarkModule {
  public Task Cleanup() {
    ultralightController.Cleanup();
    return Task.CompletedTask;
  }

  public async Task Init() {
    await ultralightController.StartAsync();
  }

  public Task Run() {
    ultralightController.Update();
    return Task.CompletedTask;
  }
}
