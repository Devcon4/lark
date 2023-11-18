using System.Collections.Frozen;
using System.Collections.Immutable;
using Lark.Engine.ecs;
using Lark.Engine.std;
using Lark.Game.components;
using Microsoft.Extensions.Logging;

namespace Lark.Game.systems;
public class LoggerSystem(ILogger<LoggerSystem> logger, EntityManager entityManager, TimeManager tm) : LarkSystem {
  public override Type[] RequiredComponents => new Type[] { typeof(MessageComponent) };
  public override Task Init() {
    logger.LogInformation("Initializing LoggerSystem...");

    //Add 100 entities
    // for (int i = 0; i < 1000; i++) {
    //   entityManager.AddEntity(
    //     new MessageComponent() {
    //       text = "Hello World " + i
    //     });
    // }

    logger.LogInformation("LoggerSystem init done.");

    return Task.CompletedTask;
  }

  public override void Update(ValueTuple<Guid, FrozenSet<ILarkComponent>> entity) {

    var (key, components) = entity;
    var message = components.Get<MessageComponent>();

    message = message with {
    };

    entityManager.UpdateEntityComponent(key, message);
  }
}
