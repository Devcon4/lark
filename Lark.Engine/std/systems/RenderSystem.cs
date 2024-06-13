using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Numerics;
using Lark.Engine.ecs;
using Lark.Engine.Model;
using Lark.Engine.pipeline;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;

namespace Lark.Engine.std;

public class RenderSystem(ILogger<RenderSystem> logger, EntityManager em, TimeManager tm, LarkVulkanData data, ModelUtils modelUtils) : LarkSystem, ILarkSystemInit, ILarkSystemBeforeDraw {
  public override int Priority => 1001;
  public override Type[] RequiredComponents => [typeof(MeshComponent), typeof(MetadataComponent), typeof(GlobalTransformComponent)];

  public Task Init() {

    return Task.CompletedTask;
  }

  public Dictionary<Guid, Guid> entityToInstance = [];
  public Dictionary<string, Guid> pathToModel = [];


  private Guid LoadModel(string path) {
    if (pathToModel.ContainsKey(path)) {
      return pathToModel[path];
    }

    logger.LogInformation("Loading model {path}", path);

    var model = modelUtils.LoadFile(path);
    data.models.Add(model.ModelId, model);
    pathToModel.Add(path, model.ModelId);

    return model.ModelId;
  }

  // delete instance
  public void DeleteInstance((Guid, HashSet<ILarkComponent>) Entity) {
    var (key, components) = Entity;

    var instanceId = entityToInstance[key];
    var instance = data.instances[instanceId];
    var model = data.models[instance.ModelId];

    model.Dispose(data);
    data.models.Remove(instance.ModelId);
    data.instances.Remove(instanceId);
    entityToInstance.Remove(key);
  }

  private void CreateInstance((Guid, FrozenSet<ILarkComponent>) Entity) {
    var (key, components) = Entity;

    var metadata = components.Get<MetadataComponent>();
    var mesh = components.Get<MeshComponent>();
    var transform = components.Get<GlobalTransformComponent>();

    var modelKey = LoadModel(mesh.Path);

    var instance = new LarkInstance {
      ModelId = modelKey,
      Transform = new LarkTransform(transform.Position.ToGeneric(), transform.Rotation.ToGeneric(), transform.Scale.ToGeneric())
    };

    entityToInstance.Add(key, instance.InstanceId);
    data.instances.Add(instance.InstanceId, instance);
  }

  public void BeforeDraw() {
    logger.LogInformation("{frame} :: RenderSystem update instances", tm.TotalFrames);
    foreach (var (key, components) in em.GetEntitiesWithComponentsSync(RequiredComponents)) {
      var transform = components.Get<GlobalTransformComponent>();
      if (!entityToInstance.ContainsKey(key)) {
        CreateInstance((key, components));
      }
      var instanceId = entityToInstance[key];
      var instance = data.instances[instanceId];
      instance.Transform = new LarkTransform(transform.Position.ToGeneric(), transform.Rotation.ToGeneric(), transform.Scale.ToGeneric());
      data.instances[instanceId] = instance;
    }
  }

}