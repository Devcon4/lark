using System.Threading.Channels;
using System.Threading.Tasks.Dataflow;
using Lark.Engine.Pipeline;
using Microsoft.Extensions.Logging;

namespace Lark.Engine.ecs;

public class SystemManager(LarkVulkanData data, ILogger<SystemManager> logger, EntityManager entityManager, IEnumerable<ILarkSystem> systems) {
  public async void Init() {
    data.sw.Start();
    foreach (var system in systems) {
      await system.Init();
    }
  }

  private readonly BoundedChannelOptions options = new(100000) {
    FullMode = BoundedChannelFullMode.Wait,
    SingleReader = true,
    SingleWriter = false
  };

  public async Task Run() {
    data.sw.Restart();

    // Extra 24 buffer.
    var totalEntities = entityManager.GetEntitiesCount() + 24;

    var block = new ActionBlock<ValueTuple<Action<ValueTuple<Guid, HashSet<ILarkComponent>>>, Guid, HashSet<ILarkComponent>>>(job => {
      var (action, key, components) = job;
      action((key, components));
    }, new ExecutionDataflowBlockOptions() {
      EnsureOrdered = false,
      BoundedCapacity = totalEntities,
    });

    options.Capacity = totalEntities;

    var channel = Channel.CreateBounded<ValueTuple<Action<ValueTuple<Guid, HashSet<ILarkComponent>>>, Guid, HashSet<ILarkComponent>>>(options);

    var readTask = Task.Run(async () => {
      var reader = channel.Reader;
      await foreach (var job in reader.ReadAllAsync()) {
        block.Post(job);
      }
    });

    var writer = channel.Writer;
    foreach (var system in systems) {
      var entities = entityManager.GetEntitiesWithComponents(system.RequiredComponents);
      foreach (var (key, components) in entities) {
        await writer.WriteAsync((system.Update, key, components));
      }
    }

    writer.Complete();
    block.Complete();

    await readTask;
    await block.Completion;

    data.sw.Stop();
    logger.LogInformation("Frame Time :: {ms}ms", data.sw.Elapsed.TotalNanoseconds / 1000000);
  }
}