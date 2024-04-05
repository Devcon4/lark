using System.Collections.Frozen;
using System.Numerics;
using Lark.Engine.ecs;
using Lark.Engine.Model;
using Lark.Engine.pipeline;
using Silk.NET.Maths;

namespace Lark.Engine.std;


public class CameraSystem(EntityManager em, LarkVulkanData data) : LarkSystem, ILarkSystemAfterUpdate {
  public override Type[] RequiredComponents => [typeof(MetadataComponent), typeof(GlobalTransformComponent), typeof(CameraComponent)];

  private Dictionary<Guid, LarkCamera> cameraLookup = new();



  public async void AfterUpdate() {
    data.cameras = cameraLookup;
    await Task.CompletedTask;
  }

  public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
    var (key, components) = Entity;
    var (metadata, transform, camera) = components.Get<MetadataComponent, GlobalTransformComponent, CameraComponent>();

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
      ViewportSize = new Vector2(data.SwapchainExtent.Width, data.SwapchainExtent.Height),
      Far = camera.Far,
      Near = camera.Near,
      Fov = camera.Fov,
      Transform = new LarkTransform(transform.Position.ToGeneric(), transform.Rotation.ToGeneric(), transform.Scale.ToGeneric()),
    };

    cameraLookup[key] = newCamera;
  }
}