using Microsoft.Extensions.DependencyInjection;

namespace Lark.Game;
public static class ServiceConfiguration {
  public static IServiceCollection AddGame(this IServiceCollection services) {
    services.AddHostedService<Game>();
    return services;
  }
}