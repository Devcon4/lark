using Lark.Engine.ecs;
using Lark.Engine.Model;
using Lark.Engine.Pipeline;
using Lark.Engine.std;
using Microsoft.Extensions.DependencyInjection;

namespace Lark.Engine;
public static class ServiceConfiguration {

  public static IServiceCollection AddLarkEngine(this IServiceCollection services) {
    services.AddSingleton<Engine>();
    services.AddSingleton<LarkWindow>();
    services.AddSingleton<ShaderBuilder>();
    services.AddSingleton<ModelBuilder>();

    services.AddLarkECS().AddLarkSTD();
    return services;
  }

  public static IServiceCollection AddLarkSTD(this IServiceCollection services) {
    services.AddSingleton<ILarkSystem, RenderSystem>();
    services.AddSingleton<ILarkSystem, CameraSystem>();
    services.AddSingleton<TimeManager>();

    return services;
  }

  // AddECSServices
  public static IServiceCollection AddLarkECS(this IServiceCollection services) {
    services.AddSingleton<EntityManager>();
    services.AddSingleton<SystemManager>();

    return services;
  }

  public static IServiceCollection AddVulkanPipeline(this IServiceCollection services) {
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