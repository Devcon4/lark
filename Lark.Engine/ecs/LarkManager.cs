namespace Lark.Engine.ecs;

public interface ILarkManagerInit {
  public abstract Task Init();
}

public interface ILarkManagerCleanup {
  public abstract Task Cleanup();
}

public interface ILarkManager : ILarkManagerInit, ILarkManagerCleanup { }

public abstract class LarkManager : ILarkManager {
  public virtual Task Cleanup() { return Task.CompletedTask; }
  public virtual Task Init() { return Task.CompletedTask; }
}