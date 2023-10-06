using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lark.Engine.ecs;

public interface ILarkComponent { }

public interface ILarkChannelOwner {
  public ChannelWriter<ValueTuple<Guid, HashSet<ILarkComponent>>> GetJobWriter();
  public ChannelReader<ValueTuple<Guid, HashSet<ILarkComponent>>> GetJobReader();
}

public interface ILarkSystem {
  public abstract Type[] RequiredComponents { get; init; }
  public abstract void Update(ValueTuple<Guid, HashSet<ILarkComponent>> Entity);
  public abstract Task Init();
}

public abstract class LarkSystem : ILarkSystem {
  public abstract Type[] RequiredComponents { get; init; }



  // _jobQueue: Replacement of _jobChannel as a ConcurrentQueue
  // private Queue<ValueTuple<Guid, HashSet<ILarkComponent>>> _jobQueue = new();
  //queueWritter 

  public LarkSystem(ILogger<LarkSystem> logger) {
    logger.LogInformation("Initializing {system}...", GetType().Name);
  }
  // Enqueue job
  // public void EnqueueJob(ValueTuple<Guid, HashSet<ILarkComponent>> job) => _jobQueue.Enqueue(job);

  public abstract void Update(ValueTuple<Guid, HashSet<ILarkComponent>> Entity);
  public abstract Task Init();
}
