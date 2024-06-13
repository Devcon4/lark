using System.Collections.Frozen;
using Lark.Engine.ecs;
using Microsoft.Extensions.Logging;

namespace Lark.Engine.std.systems {
  public class CurrentKeySystem(ActionManager am) : LarkSystem, ILarkSystemInit {
    public override Type[] RequiredComponents => [typeof(CurrentKeysInputComponent)];

    public Task Init() {
      return Task.CompletedTask;
    }

    public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
      am.UpdateAsync().Wait();
    }
  }
}