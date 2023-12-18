
using System.Collections.Frozen;
using System.Numerics;
using Lark.Engine.ecs;
using Lark.Engine.std;
using Microsoft.Extensions.Logging;
using Silk.NET.Input;

namespace Lark.Game.systems;

public class InitSystem(EntityManager em, TimeManager tm, ILogger<InitSystem> logger) : LarkSystem {
  public override Type[] RequiredComponents => [typeof(TransformComponent), typeof(CameraComponent)];

  public override Task Init() {
    var start = new TransformComponent(new(10, 0, 0), Vector3.One, Quaternion.Identity);

    // Add camera, position camera 15 units away from origin, 10 units above ground
    var cameraTransform = start with {
      Position = new(0, -5, 0),
      Rotation = Quaternion.CreateFromYawPitchRoll(0, 0, 0)
    };

    var moveForwardAction = new ActionMapComponent("MoveForward",
      new LarkKeyTrigger(LarkKeys.W, LarkInputAction.Repeat)) with {
      Action = (name, entity, input) => {
        var (key, components) = entity;
        var transform = components.Get<TransformComponent>();

        var newTransform = transform with {
          Position = transform.Position + new Vector3(0, 0, 0.01f * (float)tm.DeltaTime.TotalMilliseconds),
        };

        em.UpdateEntityComponent(key, newTransform);
      }
    };
    
    var moveBackwardAction = new ActionMapComponent("MoveBackward",
      new LarkKeyTrigger(LarkKeys.S, LarkInputAction.Repeat)) with {
      Action = (name, entity, input) => {
        var (key, components) = entity;
        var transform = components.Get<TransformComponent>();

        var newTransform = transform with {
          Position = transform.Position + new Vector3(0, 0, -0.01f * (float)tm.DeltaTime.TotalMilliseconds),
        };

        em.UpdateEntityComponent(key, newTransform);
      }
    };

    em.AddEntity(new MetadataComponent("Camera-1"), moveForwardAction, moveBackwardAction, new CameraComponent() with { Active = true }, cameraTransform);

    em.AddEntity(new MeshComponent("antiqueCamera/AntiqueCamera.gltf"), new MetadataComponent("Antique-1"), start with { Position = new(15, 1, 0) });
    em.AddEntity(new MeshComponent("testPlane/test_plane.glb"), new MetadataComponent("plane-1"), start with { Position = new(0, -10, 0) });
    return Task.CompletedTask;
  }

  public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
    var (key, components) = Entity;
    var transform = components.Get<TransformComponent>();

    // var newTransform = transform with {
    //   Position = new(0, -5, 0),
    //   Rotation = transform.Rotation * LarkUtils.CreateFromYawPitchRoll(0.0f, 0.0f, 0.0f * (float)tm.DeltaTime.TotalMilliseconds),
    // };

    // em.UpdateEntityComponent(key, newTransform);

    var camera = components.Get<CameraComponent>();
    var newCamera = camera with {
      Active = true,
      Fov = 80,
    };

    em.UpdateEntityComponent(key, newCamera);
  }
}
