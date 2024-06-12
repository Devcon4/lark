using Lark.Engine;
using Lark.Engine.ecs;
using Lark.Game.components;
using Lark.Game.managers;
using Lark.Game.systems;
using Microsoft.Extensions.DependencyInjection;

namespace Lark.Game;
public static class ServiceConfiguration {

  // AddGameSystems
  public static IServiceCollection AddGameSystems(this IServiceCollection services) {

    // services.AddLarkSystem<HeroMainAttackSystem>();
    // services.AddLarkSystem<HeroAltAttackSystem>();

    services.AddLarkSystem<InitSystem>();
    services.AddLarkSystem<CharacterSystem>();
    services.AddLarkSystem<CharacterDisplacementSystem>();

    // services.AddLarkManager<AbilitySetManager>();
    // services.AddLarkSystem<CastAbilitySystem>();
    // services.AddLarkSystem<CastInstanceSystem>();
    return services;
  }
}