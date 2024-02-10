using Lark.Engine.ecs;
using Lark.Engine.Model;
using Lark.Engine.physx.managers;
using Lark.Engine.physx.systems;
using Lark.Engine.pipeline;
using Lark.Engine.std;
using Lark.Engine.std.systems;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lark.Engine;
public static class ServiceConfiguration {

  public static IServiceCollection AddLarkPhysx(this IServiceCollection services, IConfiguration configuration) {
    services.AddSingleton<ILarkModule, PhysxModule>();

    services.Configure<LarkPhysxConfig>(configuration.GetSection("LarkPhysx"));

    services.AddSingleton<PhysxData>();
    services.AddSingleton<PhysxManager>();
    services.AddSingleton<PhysxColliderManager>();
    services.AddSingleton<PhysxCharacterManager>();

    services.AddSingleton<ILarkSystem, PhysxWorldSystem>();

    services.AddSingleton<ILarkSystem, PhysxPlaneSystem>();
    services.AddSingleton<ILarkSystem, PhysxBoxSystem>();
    services.AddSingleton<ILarkSystem, PhysxCapsuleSystem>();
    services.AddSingleton<ILarkSystem, PhysxSphereSystem>();

    services.AddSingleton<ILarkSystem, PhysxMaterialSystem>();
    services.AddSingleton<ILarkSystem, PhysxTransformSystem>();
    services.AddSingleton<ILarkSystem, PhysxRigidbodySystem>();
    services.AddSingleton<ILarkSystem, PhysxCharacterSystem>();

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
    services.AddSingleton<ILarkSystem, RenderSystem>();
    services.AddSingleton<ILarkSystem, CameraSystem>();
    services.AddSingleton<ILarkSystem, InputSystem>();
    services.AddSingleton<ILarkSystem, CurrentKeySystem>();

    services.AddSingleton<TimeManager>();
    services.AddSingleton<InputManager>();
    services.AddSingleton<ActionManager>();
    services.AddSingleton<ShutdownManager>();
    services.AddSingleton<CameraManager>();

    services.AddSingleton<ILarkModule, ActionModule>();

    return services;
  }

  // AddECSServices
  public static IServiceCollection AddLarkECS(this IServiceCollection services) {
    services.AddSingleton<ILarkModule, EcsModule>();
    services.AddSingleton<EntityManager>();
    services.AddSingleton<SystemManager>();

    return services;
  }

  public static IServiceCollection AddVulkanPipeline(this IServiceCollection services) {
    services.AddSingleton<ILarkModule, VulkanModule>();

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