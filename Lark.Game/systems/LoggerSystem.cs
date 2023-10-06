using Lark.Engine.ecs;
using Lark.Game.components;
using Microsoft.Extensions.Logging;

namespace Lark.Game.systems;
public class LoggerSystem(ILogger<LoggerSystem> logger, EntityManager entityManager) : LarkSystem(logger) {
  public override Type[] RequiredComponents { get; init; } = new Type[] { typeof(MessageComponent) };
  public override Task Init() {
    logger.LogInformation("Initializing LoggerSystem...");

    //Add 100 entities
    for (int i = 0; i < 1000; i++) {
      entityManager.AddEntity(
        new MessageComponent() {
          text = "Hello World " + i
        });
    }

    logger.LogInformation("LoggerSystem init done.");

    return Task.CompletedTask;
  }

  public override void Update(ValueTuple<Guid, HashSet<ILarkComponent>> entity) {

    var (key, components) = entity;
    var message = components.Get<MessageComponent>();

    logger.LogDebug("Entity :: {key} says {message}", key, message.text);

    message = message with {
      text = message.text + DateTime.Now
    };

    entityManager.UpdateEntityComponent(key, message);
  }
}
