using Lark.Engine;
using Microsoft.Extensions.DependencyInjection;

public static class ServiceConfiguration {

  public static IServiceCollection AddGame(this IServiceCollection services) {
    services.AddHostedService<Game>();
    return services;
  }

  public static IServiceCollection AddLarkEngine(this IServiceCollection services) {
    services.AddSingleton<Engine>();
    services.AddSingleton<VulkanBuilder>();
    services.AddSingleton<LarkWindow>();
    return services;
  }
}