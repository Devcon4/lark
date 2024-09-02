using System.Collections.Frozen;
using System.Numerics;
using Lark.Engine.ecs;
using Lark.Engine.model;
using Lark.Engine.pipeline;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;

namespace Lark.Engine.std;


public class CameraSystem(EntityManager em, LarkVulkanData data) : LarkSystem, ILarkSystemBeforeDraw {
  public override int Priority => 1000;
  public override Type[] RequiredComponents => [typeof(GlobalTransformComponent), typeof(CameraComponent)];

  private Dictionary<Guid, LarkCamera> cameraLookup = new();

  public async void BeforeDraw() {
    foreach (var (key, components) in em.GetEntitiesWithComponentsSync(RequiredComponents)) {
      var (transform, camera) = components.Get<GlobalTransformComponent, CameraComponent>();

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

    data.cameras = cameraLookup;
    await Task.CompletedTask;
  }
}