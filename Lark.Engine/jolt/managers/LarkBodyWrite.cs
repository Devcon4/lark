using JoltPhysicsSharp;

namespace Lark.Engine.jolt.managers;

public class LarkBodyWrite : IDisposable {
  public Body Instance { get; private set; }
  private readonly BodyLockWrite _blw;
  private readonly BodyLockInterface _bi;
  public LarkBodyWrite(BodyLockInterface bi, BodyID bodyId) {
    _bi = bi;
    _bi.LockWrite(bodyId, out _blw);
    Instance = _blw.Body;
  }

  public void Dispose() {
    GC.SuppressFinalize(this);
    _bi.UnlockWrite(_blw);
  }
}
