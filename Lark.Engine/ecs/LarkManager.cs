namespace Lark.Engine.ecs;

public interface ILarkManagerInit {
  public abstract Task Init();
}

public interface ILarkManagerCleanup {
  public abstract Task Cleanup();
}

public interface ILarkManagerAfterInit {
  public abstract Task AfterInit();
}

public interface ILarkManager : ILarkManagerInit, ILarkManagerCleanup, ILarkManagerAfterInit { }

public abstract class LarkManager : ILarkManager {
  public virtual Task Cleanup() { return Task.CompletedTask; }
  public virtual Task Init() { return Task.CompletedTask; }
  public virtual Task AfterInit() { return Task.CompletedTask; }
}