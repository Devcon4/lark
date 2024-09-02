using System.Collections.Frozen;
using System.Numerics;
using Lark.Engine.ecs;
using Lark.Engine.model;
using Lark.Engine.pipeline;
using Lark.Engine.std;
using Microsoft.Extensions.Logging;

namespace Lark.Engine.gi;

public class PointLightSystem(LightProbeManager pm, LarkVulkanData shareData, ILogger<PointLightSystem> logger) : LarkSystem {
  public override Type[] RequiredComponents => [typeof(TransformComponent), typeof(LightComponent)];

  private readonly Dictionary<Vector3, Guid> _lightPositions = [];
  private readonly Dictionary<Guid, Guid> _entityToLightId = [];

  public override void Update((Guid, FrozenSet<ILarkComponent>) entity) {
    var (key, components) = entity;

    var transform = components.Get<TransformComponent>();
    if (_lightPositions.ContainsKey(transform.Position)) return;

    if (!_entityToLightId.ContainsKey(key)) {
      var light = components.Get<LightComponent>();
      var newLightModel = new LarkLight() {
        Settings = light.Settings,
        Transform = new(transform),
      };
      _entityToLightId.Add(key, newLightModel.LightId);
      _lightPositions.Add(transform.Position, key);
      shareData.lights.Add(newLightModel.LightId, newLightModel);
      pm.RegisterLight(transform.Position);
      logger.LogInformation("Registered new light :: {position}", transform.Position);
      return;
    }

    var lastPosition = _lightPositions.FirstOrDefault(p => p.Value == key).Key;

    if (lastPosition == default) {
      throw new Exception($"PointLight not found with key: {key}; This should never happen.");
    }

    var lightModel = shareData.lights[_entityToLightId[key]];
    lightModel.Transform = new(transform);
    // The light has moved, update the probe manager
    pm.MoveLight(lastPosition, transform.Position);
    _lightPositions.Remove(lastPosition);
    _lightPositions.Add(transform.Position, key);
  }
}

// public class PointLightInstanceSystem(LightProbeManager pm) : LarkSystem {
//   public override Type[] RequiredComponents => [typeof(TransformComponent), typeof(LightInstanceComponent)];

//   private readonly Dictionary<Vector3, Guid> _lightPositions = [];

//   public override void Update((Guid, FrozenSet<ILarkComponent>) entity) {
//     var (key, components) = entity;

//     var transform = components.Get<TransformComponent>();
//     if (_lightPositions.ContainsKey(transform.Position)) return;

//     var lastPosition = _lightPositions.FirstOrDefault(p => p.Value == key).Key;

//     if (lastPosition == default) {
//       throw new Exception($"PointLight not found with key: {key}; This should never happen.");
//     }

//     // The light has moved, update the probe manager
//     pm.MoveLight(lastPosition, transform.Position);

//   }
// }