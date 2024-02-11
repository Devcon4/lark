namespace Lark.Engine.physx.managers;

public class PhysxModule(PhysxManager pm) : ILarkModule {
  public Task Cleanup() {
    pm.Dispose();
    return Task.CompletedTask;
  }

  public Task Init() {
    return Task.CompletedTask;
  }

  public Task Run() {
    pm.SimulateFrame();
    return Task.CompletedTask;
  }
}
