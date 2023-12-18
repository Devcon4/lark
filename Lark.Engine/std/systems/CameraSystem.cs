using System.Collections.Frozen;
using Lark.Engine.ecs;
using Lark.Engine.Model;
using Lark.Engine.Pipeline;
using Silk.NET.Maths;

namespace Lark.Engine.std;

public class CameraSystem(EntityManager em, LarkVulkanData data) : LarkSystem {
  public override Type[] RequiredComponents => [typeof(MetadataComponent), typeof(TransformComponent), typeof(CameraComponent)];

  private Dictionary<Guid, LarkCamera> cameraLookup = new();

  public override Task Init() {
    return Task.CompletedTask;
  }

  public override void AfterUpdate() {
    data.cameras = cameraLookup;
  }

  public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
    var (key, components) = Entity;
    var (metadata, transform, camera) = components.Get<MetadataComponent, TransformComponent, CameraComponent>();

    if (!cameraLookup.ContainsKey(key)) {
      cameraLookup.Add(key, LarkCamera.DefaultCamera());
    }

    if (!camera.FixedAspectRatio) {
      camera = camera with {
        AspectRatio = data.SwapchainExtent.Width / (float)data.SwapchainExtent.Height,
      };

      em.UpdateEntityComponent(key, camera);
    }

    var newCamera = cameraLookup[key] with {
      Active = camera.Active,
      AspectRatio = camera.AspectRatio,
      Far = camera.Far,
      Near = camera.Near,
      Fov = camera.Fov,
      Transform = new LarkTransform(transform)
    };

    cameraLookup[key] = newCamera;
  }
}