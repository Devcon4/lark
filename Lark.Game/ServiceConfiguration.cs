using Lark.Engine.ecs;
using Lark.Game.components;
using Lark.Game.systems;
using Microsoft.Extensions.DependencyInjection;

namespace Lark.Game;
public static class ServiceConfiguration {

  // AddGameSystems
  public static IServiceCollection AddGameSystems(this IServiceCollection services) {
    services.AddSingleton<ILarkSystem, LoggerSystem>();
    services.AddSingleton<ILarkSystem, PhysicsSystem>();
    services.AddSingleton<ILarkSystem, InitSystem>();
    services.AddSingleton<ILarkSystem, JumpSystem>();
    services.AddSingleton<ILarkSystem, VelocitySystem>();
    services.AddSingleton<ILarkSystem, PhysxInitSystem>();
    return services;
  }
}