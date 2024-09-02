using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using System.Threading.Tasks.Dataflow;
using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Helpers;
using Lark.Engine.pipeline;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Silk.NET.OpenGL;
using Silk.NET.Vulkan;

namespace Lark.Engine.ecs;

public readonly struct UpdateJob : IAction2D {
  private readonly ILarkSystem[] systems;
  private readonly (Guid, FrozenSet<ILarkComponent>, FrozenSet<Type>)[] entities;

  public UpdateJob(ref ILarkSystem[] systems, ref (Guid, FrozenSet<ILarkComponent>, FrozenSet<Type>)[] entities) {
    this.systems = systems;
    this.entities = entities;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public readonly void Invoke(int i, int j) {
    var system = systems[i];
    var (key, components, types) = entities[j];
    var hasGeneric = types.Any(t => t.IsGenericType);

    if (!hasGeneric) {
      if (types.IsSupersetOf(system.RequiredComponents)) {
        system.Update((key, components));
        return;
      }
    }

    // If componentTypes has any generic types, we need to check the inheritance chain.
    if (hasGeneric) {
      foreach (var componentType in system.RequiredComponents) {
        if (componentType.IsGenericType) {
          // EntityCompoents will have full type records like Typeof(CastAbility<HeroMainAttack>), typeof(CastAbility<HeroAltAttack>), etc. In this example if we are looking for CastAbility<>, we need to check for all types that inherit from CastAbility<>.
          if (types.Any(t => t.IsGenericType && (t.GetGenericTypeDefinition() == componentType || t.IsAssignableFrom(componentType)))) {
            system.Update((key, components));
            return;
          }
        }
      }
    }
  }
}

public class SystemManager(
  LarkVulkanData data,
  ILogger<SystemManager> logger,
  EntityManager entityManager,
  IEnumerable<ILarkSystem> systems,
  IEnumerable<ILarkSyncSystem> syncSystems) : LarkManager {
  public override async Task AfterInit() {
    // Systems must be initialized after managers.
    // var _ = Task.Run(async () => {

    // });

    // return Task.CompletedTask;

    // Log registered systems and their priorities
    foreach (var system in systems.OrderByDescending(s => s.Priority)) {
      logger.LogInformation("Registered system {system} :: {priority}", system.GetType().Name, system.Priority);
    }


    data.sw.Start();

    var systemInits = systems.Select(async s => {
      if (s is ILarkSystemInit initSystem) {
        await initSystem.Init();
      }
    });

    var syncSystemInits = syncSystems.Select(async s => {
      if (s is ILarkSystemInit initSystem) {
        await initSystem.Init();
      }
    });

    // suppressing for now, we intentionally want to not await this with the current setup.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    var _ = Task.Run(async () => {
      Task.WhenAll(systemInits).Wait();
      Task.WhenAll(syncSystemInits).Wait();
    });
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously


    await Task.CompletedTask;
  }

  private readonly BoundedChannelOptions options = new(100000) {
    FullMode = BoundedChannelFullMode.Wait,
    SingleReader = false,
    SingleWriter = false,
  };

  // private List<Task> BeforeUpdates => systems.Select(async s => await Task.Run(() => s.BeforeUpdate())).ToList();
  // private List<Task> AfterUpdates => systems.Select(async s => await Task.Run(() => s.AfterUpdate())).ToList();

  private readonly List<ILarkSystemBeforeUpdate> BeforeUpdateSystems = systems.OrderByDescending(s => s.Priority).Where(s => s is ILarkSystemBeforeUpdate).Cast<ILarkSystemBeforeUpdate>().ToList();
  private readonly List<ILarkSystemAfterUpdate> AfterUpdateSystems = systems.OrderByDescending(s => s.Priority).Where(s => s is ILarkSystemAfterUpdate).Cast<ILarkSystemAfterUpdate>().ToList();
  private readonly List<ILarkSystemBeforeDraw> BeforeDrawSystems = systems.OrderByDescending(s => s.Priority).Where(s => s is ILarkSystemBeforeDraw).Cast<ILarkSystemBeforeDraw>().ToList();

  public async Task RunParallelHelper() {
    foreach (var system in BeforeUpdateSystems) {
      system.BeforeUpdate();
    }

    foreach (var system in systems) {
      var entities = entityManager.GetEntitiesWithComponentsSync(system.RequiredComponents);

      foreach (var (key, components) in entities) {
        system.Update((key, components));
      }
    }
    var entitySpan = entityManager.GetEntities().ToArray();
    var systemSpan = systems.ToArray();
    var entityRange = new Range(0, entitySpan.Length);
    var systemRange = new Range(0, systemSpan.Length);
    ParallelHelper.For2D(systemRange, entityRange, new UpdateJob(ref systemSpan, ref entitySpan));

    foreach (var system in AfterUpdateSystems) {
      system.AfterUpdate();
    }

    foreach (var system in BeforeDrawSystems) {
      system.BeforeDraw();
    }

    await Task.CompletedTask;
  }

  public async Task Run() {

    // var maxCount = entityManager.GetEntitiesCount() * systems.Count();

    // options.Capacity = maxCount;

    // var updateChannel = Channel.CreateBounded<ValueTuple<Action<ValueTuple<Guid, FrozenSet<ILarkComponent>>>, Guid, FrozenSet<ILarkComponent>>>(options);

    // var producer = Task.Run(async () => {
    //   foreach (var system in systems) {
    //     var entities = entityManager.GetEntitiesWithComponents(system.RequiredComponents);

    //     await foreach (var (key, components) in entities) {
    //       await updateChannel.Writer.WriteAsync((system.Update, key, components));
    //     }
    //   }

    //   updateChannel.Writer.Complete();
    // });
    // Parallel.ForEach(BeforeUpdates, async (task) => await task);

    foreach (var system in BeforeUpdateSystems) {
      system.BeforeUpdate();
    }


    foreach (var system in systems) {
      var entities = entityManager.GetEntitiesWithComponentsSync(system.RequiredComponents);

      foreach (var (key, components) in entities) {
        system.Update((key, components));
      }

    }

    foreach (var system in AfterUpdateSystems) {
      system.AfterUpdate();
    }

    foreach (var system in BeforeDrawSystems) {
      system.BeforeDraw();
    }

    // var consumer = Task.Run(async () => {
    //   await foreach (var job in updateChannel.Reader.ReadAllAsync()) {
    //     job.Item1((job.Item2, job.Item3));
    //   }
    // });

    // await Task.WhenAll(producer, consumer);

    // Parallel.ForEach(AfterUpdates, async (task) => await task);

    await Task.CompletedTask;
  }


  public async Task RunOldest() {

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
        using var entityId = LogContext.PushProperty("EntityId", job.Item2);
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
      // await beforeUpdateWriter.WriteAsync(system.BeforeUpdate);

      // await foreach (var (key, components) in entities) {
      //   await updateWriter.WriteAsync((system.Update, key, components));
      // }

      // await afterUpdateWriter.WriteAsync(system.AfterUpdate);

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