using Lark.Engine;
using Microsoft.Extensions.DependencyInjection;

public static class ServiceConfiguration {

  public static IServiceCollection AddLarkEngine(this IServiceCollection services) {
    services.AddSingleton<Engine>();
    services.AddSingleton<VulkanBuilder>();
    services.AddSingleton<LarkWindow>();
    services.AddSingleton<ShaderBuilder>();
    return services;
  }
}