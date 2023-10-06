using Lark.Engine.ecs;
using Lark.Game.systems;
using Microsoft.Extensions.DependencyInjection;

namespace Lark.Game;
public static class ServiceConfiguration {
  public static IServiceCollection AddGame(this IServiceCollection services) {
    services.AddHostedService<Game>();
    return services;
  }

  // AddGameSystems
  public static IServiceCollection AddGameSystems(this IServiceCollection services) {
    services.AddSingleton<ILarkSystem, LoggerSystem>();
    return services;
  }
}