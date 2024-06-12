using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lark.Engine.ecs;

public interface ILarkComponent { }

public interface ILarkChannelOwner {
  public ChannelWriter<ValueTuple<Guid, FrozenSet<ILarkComponent>>> GetJobWriter();
  public ChannelReader<ValueTuple<Guid, FrozenSet<ILarkComponent>>> GetJobReader();
}

public interface ILarkSystemBeforeUpdate {
  public abstract void BeforeUpdate();
}

public interface ILarkSystemAfterUpdate {
  public abstract void AfterUpdate();
}

public interface ILarkSystemBeforeDraw {
  public abstract void BeforeDraw();
}

public interface ILarkSystemInit {
  public abstract Task Init();
}

public interface ILarkSystem {
  public abstract int Priority { get; }
  public abstract Type[] RequiredComponents { get; }
  public abstract void Update(ValueTuple<Guid, FrozenSet<ILarkComponent>> Entity);
}

public interface ILarkSyncSystem : ILarkSystem { }

public abstract class LarkSystem : ILarkSystem, ILarkSyncSystem {
  public virtual int Priority => 1024;
  public abstract Type[] RequiredComponents { get; }



  // _jobQueue: Replacement of _jobChannel as a ConcurrentQueue
  // private Queue<ValueTuple<Guid, HashSet<ILarkComponent>>> _jobQueue = new();
  //queueWritter 

  public LarkSystem() { }
  // Enqueue job
  // public void EnqueueJob(ValueTuple<Guid, HashSet<ILarkComponent>> job) => _jobQueue.Enqueue(job);

  // Called for each entity with RequiredComponents every frame
  public virtual void Update(ValueTuple<Guid, FrozenSet<ILarkComponent>> Entity) { }

}


