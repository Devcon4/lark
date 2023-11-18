using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Numerics;
using Lark.Engine.ecs;
using Lark.Engine.Model;
using Lark.Engine.Pipeline;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;

using SkiaSharp;

namespace Lark.Engine.std;

public class RenderSystem(ILogger<RenderSystem> logger, EntityManager em, TimeManager tm, LarkVulkanData data, ModelUtils modelUtils) : LarkSystem {
  public override Type[] RequiredComponents => new Type[] { typeof(MeshComponent), typeof(MetadataComponent), typeof(TransformComponent) };

  public override Task Init() {
    SKCanvas canvas = new SKCanvas(new SKBitmap(1920, 1080));
    canvas.Clear(SKColors.Black);
    // draw a crosshair in the center of the screen
    canvas.DrawLine(960, 540 - 10, 960, 540 + 10, new SKPaint { Color = SKColors.White, StrokeWidth = 2 });
    canvas.DrawLine(960 - 10, 540, 960 + 10, 540, new SKPaint { Color = SKColors.White, StrokeWidth = 2 });
    canvas.Flush();
    canvas.Save();

    return Task.CompletedTask;
  }

  public Dictionary<Guid, Guid> entityToInstance = new();
  public Dictionary<string, Guid> pathToModel = new();

  public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
    var (key, components) = Entity;

    var transform = components.Get<TransformComponent>();
    if (!entityToInstance.ContainsKey(key)) {
      CreateInstance(Entity);
    }
    var instanceId = entityToInstance[key];
    var instance = data.instances[instanceId];
    instance.Transform = new LarkTransform(transform.Position.ToGeneric(), transform.Rotation.ToGeneric(), transform.Scale.ToGeneric());
    data.instances[instanceId] = instance;
  }

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
    var transform = components.Get<TransformComponent>();

    var modelKey = LoadModel(mesh.Path);

    var instance = new LarkInstance {
      ModelId = modelKey,
      Transform = new LarkTransform(transform.Position.ToGeneric(), transform.Rotation.ToGeneric(), transform.Scale.ToGeneric())
    };

    entityToInstance.Add(key, instance.InstanceId);
    data.instances.Add(instance.InstanceId, instance);
  }
}