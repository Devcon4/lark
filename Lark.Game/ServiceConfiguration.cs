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

    services.AddLarkSystem<InitSystem>();
    services.AddLarkSystem<JumpSystem>();
    services.AddLarkSystem<VelocitySystem>();
    services.AddLarkSystem<PhysxInitSystem>();

    services.AddLarkSystem<HeroMainAttackSystem>();
    services.AddLarkSystem<HeroAltAttackSystem>();

    services.AddLarkSystem<CharacterSystem>();
    services.AddLarkSystem<CharacterTransformSystem>();
    services.AddLarkSystem<CharacterJumpSystem>();

    services.AddLarkManager<AbilitySetManager>();
    services.AddLarkSystem<CastAbilitySystem>();
    services.AddLarkSystem<CastInstanceSystem>();
    return services;
  }
}