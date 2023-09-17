using Lark.Engine.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace Lark.Engine;
public static class ServiceConfiguration {

  public static IServiceCollection AddLarkEngine(this IServiceCollection services) {
    services.AddSingleton<Engine>();
    services.AddSingleton<LarkWindow>();
    services.AddSingleton<ShaderBuilder>();
    services.AddSingleton<ModelBuilder>();
    return services;
  }

  public static IServiceCollection AddVulkanPipeline(this IServiceCollection services) {
    services.AddSingleton<VulkanBuilder>();
    services.AddSingleton<LarkVulkanData>();
    services.AddSingleton<SwapchainSupportUtil>();
    services.AddSingleton<QueueFamilyUtil>();

    services.AddSingleton<InstanceSegment>();
    services.AddSingleton<DebugSegment>();
    services.AddSingleton<SurfaceSegment>();
    services.AddSingleton<PhysicalDeviceSegment>();
    services.AddSingleton<LogicalDeviceSegment>();
    services.AddSingleton<SwapchainSegment>();
    services.AddSingleton<ImageViewSegment>();
    services.AddSingleton<RenderPassSegment>();
    services.AddSingleton<GraphicsPipelineSegment>();
    services.AddSingleton<FramebufferSegment>();
    services.AddSingleton<CommandPoolSegment>();
    services.AddSingleton<CommandBufferSegment>();
    services.AddSingleton<SyncSegment>();
    return services;
  }
}