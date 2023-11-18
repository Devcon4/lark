using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Threading.Channels;
using System.Threading.Tasks.Dataflow;
using Lark.Engine.Pipeline;
using Microsoft.Extensions.Logging;

namespace Lark.Engine.ecs;

public class SystemManager(
  LarkVulkanData data,
  ILogger<SystemManager> logger,
  EntityManager entityManager,
  IEnumerable<ILarkSystem> systems,
  IEnumerable<ILarkSyncSystem> syncSystems) {
  public async void Init() {
    data.sw.Start();
    foreach (var system in systems) {
      await system.Init();
    }

    foreach (var system in syncSystems) {
      await system.Init();
    }
  }

  private readonly BoundedChannelOptions options = new(100000) {
    FullMode = BoundedChannelFullMode.Wait,
    SingleReader = true,
    SingleWriter = false
  };

  public async Task Run() {

    // Extra 24 buffer.
    var totalEntities = entityManager.GetEntitiesCount() + 24;

    // var block = new ActionBlock<ValueTuple<Action<ValueTuple<Guid, FrozenSet<ILarkComponent>>>, Guid, FrozenSet<ILarkComponent>>>(job => {
    //   var (action, key, components) = job;
    //   action((key, components));
    // }, new ExecutionDataflowBlockOptions() {
    //   EnsureOrdered = false,
    //   BoundedCapacity = totalEntities
    // });

    options.Capacity = totalEntities;

    var channel = Channel.CreateBounded<ValueTuple<Action<ValueTuple<Guid, FrozenSet<ILarkComponent>>>, Guid, FrozenSet<ILarkComponent>>>(options);

    var readTask = Task.Run(async () => {
      var reader = channel.Reader;
      await foreach (var job in reader.ReadAllAsync()) {
        // job.Item1((job.Item2, job.Item3));
        // block.Post(job);
        job.Item1((job.Item2, job.Item3));
      }
    });

    var writer = channel.Writer;
    foreach (var system in systems) {
      var entities = entityManager.GetEntitiesWithComponents(system.RequiredComponents);

      await foreach (var (key, components) in entities) {
        await writer.WriteAsync((system.Update, key, components));
      }

      // await Parallel.ForEachAsync(entities, async (entity, ct) => {
      //   var (key, components) = entity;
      //   await writer.WriteAsync((system.Update, key, components), ct);
      // });

      // foreach (var (key, components) in entities) {
      //   await writer.WriteAsync((system.Update, key, components));
      // }
    }

    writer.Complete();
    // block.Complete();

    await readTask;
    // await block.Completion;

    // foreach (var system in syncSystems) {
    //   var entities = entityManager.GetEntitiesWithComponents(system.RequiredComponents);
    //   foreach (var (key, components) in entities) {
    //     system.Update((key, components));
    //   }
    // }
  }
}