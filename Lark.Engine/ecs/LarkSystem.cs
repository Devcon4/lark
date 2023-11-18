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

public interface ILarkSystem {
  public abstract Type[] RequiredComponents { get; }
  public abstract void Update(ValueTuple<Guid, FrozenSet<ILarkComponent>> Entity);
  public abstract Task Init();
}

public interface ILarkSyncSystem : ILarkSystem { }

public abstract class LarkSystem : ILarkSystem, ILarkSyncSystem {
  public abstract Type[] RequiredComponents { get; }



  // _jobQueue: Replacement of _jobChannel as a ConcurrentQueue
  // private Queue<ValueTuple<Guid, HashSet<ILarkComponent>>> _jobQueue = new();
  //queueWritter 

  public LarkSystem() { }
  // Enqueue job
  // public void EnqueueJob(ValueTuple<Guid, HashSet<ILarkComponent>> job) => _jobQueue.Enqueue(job);

  public abstract void Update(ValueTuple<Guid, FrozenSet<ILarkComponent>> Entity);
  public abstract Task Init();
}


