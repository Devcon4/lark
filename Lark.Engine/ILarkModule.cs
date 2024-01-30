namespace Lark.Engine;

public interface ILarkModule {
  public Task Init();
  public Task Run();
  public Task Cleanup();
}
