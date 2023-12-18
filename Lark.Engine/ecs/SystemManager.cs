using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Threading.Channels;
using System.Threading.Tasks.Dataflow;
using Lark.Engine.Pipeline;
using Microsoft.Extensions.Logging;
using Silk.NET.Vulkan;

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

    var beforeUpdateChannel = Channel.CreateBounded<Action>(options);
    var updateChannel = Channel.CreateBounded<ValueTuple<Action<ValueTuple<Guid, FrozenSet<ILarkComponent>>>, Guid, FrozenSet<ILarkComponent>>>(options);
    var afterUpdateChannel = Channel.CreateBounded<Action>(options);

    var beforeUpdateReadTask = Task.Run(async () => {
      var reader = beforeUpdateChannel.Reader;
      await foreach (var job in reader.ReadAllAsync()) {
        job();
      }
    });

    var updateReadTask = Task.Run(async () => {
      var reader = updateChannel.Reader;
      await foreach (var job in reader.ReadAllAsync()) {
        // job.Item1((job.Item2, job.Item3));
        // block.Post(job);
        job.Item1((job.Item2, job.Item3));
      }
    });

    var afterUpdateReadTask = Task.Run(async () => {
      var reader = afterUpdateChannel.Reader;
      await foreach (var job in reader.ReadAllAsync()) {
        job();
      }
    });

    var beforeUpdateWriter = beforeUpdateChannel.Writer;
    var updateWriter = updateChannel.Writer;
    var afterUpdateWriter = afterUpdateChannel.Writer;
    foreach (var system in systems) {
      var entities = entityManager.GetEntitiesWithComponents(system.RequiredComponents);

      // If system has a BeforeUpdate method, add it to the beforeUpdateChannel
      await beforeUpdateWriter.WriteAsync(system.BeforeUpdate);

      await foreach (var (key, components) in entities) {
        await updateWriter.WriteAsync((system.Update, key, components));
      }

      await afterUpdateWriter.WriteAsync(system.AfterUpdate);

      // await Parallel.ForEachAsync(entities, async (entity, ct) => {
      //   var (key, components) = entity;
      //   await writer.WriteAsync((system.Update, key, components), ct);
      // });

      // foreach (var (key, components) in entities) {
      //   await writer.WriteAsync((system.Update, key, components));
      // }
    }

    beforeUpdateWriter.Complete();
    updateWriter.Complete();
    afterUpdateWriter.Complete();
    // block.Complete();

    await beforeUpdateReadTask;
    await updateReadTask;
    await afterUpdateReadTask;

    // await block.Completion;

    // foreach (var system in syncSystems) {
    //   var entities = entityManager.GetEntitiesWithComponents(system.RequiredComponents);
    //   foreach (var (key, components) in entities) {
    //     system.Update((key, components));
    //   }
    // }
  }
}