using Lark.Engine.ecs;
using Lark.Engine.Model;
using Lark.Engine.physx.managers;
using Lark.Engine.physx.systems;
using Lark.Engine.pipeline;
using Lark.Engine.std;
using Lark.Engine.std.systems;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;

namespace Lark.Engine;
public static class ServiceConfiguration {

  // Managers can be injected directly.
  public static IServiceCollection AddLarkManager<T>(this IServiceCollection services) where T : LarkManager {
    services.AddSingleton<T>();
    services.AddSingleton<ILarkManager, T>(sp => sp.GetRequiredService<T>());
    return services;
  }

  public static IServiceCollection AddLarkSystem<T>(this IServiceCollection services) where T : LarkSystem {
    services.AddSingleton<ILarkSystem, T>();
    return services;
  }

  public static IServiceCollection AddLarkModule<T>(this IServiceCollection services) where T : class, ILarkModule {
    services.AddSingleton<ILarkModule, T>();
    return services;
  }

  public static IServiceCollection AddLarkPhysx(this IServiceCollection services, IConfiguration configuration) {
    services.AddSingleton<ILarkModule, PhysxModule>();

    services.Configure<LarkPhysxConfig>(configuration.GetSection("LarkPhysx"));

    services.AddSingleton<PhysxData>();
    services.AddLarkManager<PhysxManager>();
    services.AddLarkManager<PhysxColliderManager>();
    services.AddLarkManager<PhysxCharacterManager>();

    // services.AddLarkSystem<PhysxWorldSystem>();

    services.AddLarkSystem<PhysxPlaneSystem>();
    services.AddLarkSystem<PhysxBoxSystem>();
    services.AddLarkSystem<PhysxCapsuleSystem>();
    services.AddLarkSystem<PhysxSphereSystem>();

    services.AddLarkSystem<PhysxMaterialSystem>();
    services.AddLarkSystem<PhysxTransformSystem>();
    services.AddLarkSystem<PhysxRigidbodySystem>();
    services.AddLarkSystem<PhysxCharacterSystem>();

    return services;
  }

  public static IServiceCollection AddLarkEngine(this IServiceCollection services, IConfiguration configuration) {
    services.AddSingleton<Engine>();
    services.AddSingleton<LarkWindow>();
    services.AddSingleton<ShaderBuilder>();
    services.AddSingleton<ModelBuilder>();

    services.Configure<GameSettings>(configuration.GetSection("GameSettings"));


    return services;
  }

  public static IServiceCollection AddLarkSTD(this IServiceCollection services) {
    services.AddLarkSystem<RenderSystem>();
    services.AddLarkSystem<CameraSystem>();
    services.AddLarkSystem<InputSystem>();
    services.AddLarkSystem<CurrentKeySystem>();
    services.AddLarkSystem<SceneGraphSystem>();
    services.AddLarkSystem<GlobalTransformSystem>();

    services.AddSingleton<IClock>(SystemClock.Instance);
    services.AddLarkManager<TimeManager>();
    services.AddLarkManager<ActionManager>();
    services.AddLarkManager<InputManager>();
    services.AddLarkManager<ShutdownManager>();
    services.AddLarkManager<CameraManager>();
    services.AddLarkManager<SceneGraphManager>();

    services.AddLarkModule<ActionModule>();

    return services;
  }

  // AddECSServices
  public static IServiceCollection AddLarkECS(this IServiceCollection services) {
    services.AddLarkModule<EcsModule>();
    services.AddLarkManager<EntityManager>();
    services.AddLarkManager<SystemManager>();

    return services;
  }

  public static IServiceCollection AddVulkanPipeline(this IServiceCollection services) {
    services.AddLarkModule<VulkanModule>();

    services.AddSingleton<LarkVulkanData>();
    services.AddSingleton<QueueFamilyUtil>();
    services.AddSingleton<BufferUtils>();
    services.AddSingleton<CommandUtils>();
    services.AddSingleton<ImageUtils>();

    services.AddSingleton<VulkanBuilder>();
    services.AddSingleton<SwapchainSupportUtil>();
    services.AddSingleton<InstanceSegment>();
    services.AddSingleton<DebugSegment>();
    services.AddSingleton<SurfaceSegment>();
    services.AddSingleton<PhysicalDeviceSegment>();
    services.AddSingleton<LogicalDeviceSegment>();
    services.AddSingleton<SwapchainSegment>();
    services.AddSingleton<UniformBufferSegment>();
    services.AddSingleton<ImageViewSegment>();
    services.AddSingleton<RenderPassSegment>();
    services.AddSingleton<DescriptorSetSegment>();
    services.AddSingleton<GraphicsPipelineSegment>();
    services.AddSingleton<FramebufferSegment>();
    services.AddSingleton<TextureSegment>();
    services.AddSingleton<SamplerSegment>();
    services.AddSingleton<CommandPoolSegment>();
    services.AddSingleton<CommandBufferSegment>();
    services.AddSingleton<SyncSegment>();
    services.AddSingleton<MeshBufferSegment>();
    services.AddSingleton<DepthSegment>();

    services.AddSingleton<ModelUtils>();
    services.AddSingleton<ModelBuilder>();

    return services;
  }
}