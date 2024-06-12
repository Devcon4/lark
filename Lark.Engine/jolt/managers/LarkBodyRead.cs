using JoltPhysicsSharp;

namespace Lark.Engine.jolt.managers;

public class LarkBodyRead : IDisposable
{
  public Body Instance { get; private set; }
  private readonly BodyLockRead _blw;
  private readonly BodyLockInterface _bi;
  public LarkBodyRead(BodyLockInterface bi, BodyID bodyId)
  {
    _bi = bi;
    _bi.LockRead(bodyId, out _blw);
    Instance = _blw.Body;
  }

  public void Dispose()
  {
    GC.SuppressFinalize(this);
    _bi.UnlockRead(_blw);
  }
}
