using Lark.Engine.ecs;
using Lark.Engine.jolt.managers;

namespace Lark.Engine.jolt;
public class JoltModule(JoltManager jm) : ILarkModule {
  public Task Cleanup() {
    return Task.CompletedTask;
  }

  public Task Init() {
    return Task.CompletedTask;
  }

  public Task Run() {
    jm.SimulateAllSystems();
    return Task.CompletedTask;
  }
}
