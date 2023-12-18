
using System.Collections.Frozen;
using System.Security.Cryptography;
using Lark.Engine.ecs;
using Lark.Engine.std;
using Microsoft.Extensions.Logging;

namespace Lark.Game.systems;

public class ActionSystem(ILogger<ActionSystem> logger): LarkSystem {
  public override Type[] RequiredComponents => [typeof(SystemComponent), typeof(CurrentKeyInputComponent)];

  public override Task Init() {

    return Task.CompletedTask;
  }

  public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
    var (key, components) = Entity;
    var (system, input) = components.Get<SystemComponent, CurrentKeyInputComponent>();

    switch (input.KeyActions) {
      case (LarkKeys.W, LarkInputAction.Press):
        logger.LogInformation("W pressed");
        break;
      case (LarkKeys.W, LarkInputAction.Release):
        logger.LogInformation("W released");
        break;
      case (LarkKeys.A, LarkInputAction.Press):
        logger.LogInformation("A pressed");
        break;
      case (LarkKeys.A, LarkInputAction.Release):
        logger.LogInformation("A released");
        break;
    }
  }
}