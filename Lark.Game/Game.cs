//An IHostedService that runs the engine and begins the game.

using Microsoft.Extensions.Hosting;

namespace Lark.Game;
public class Game(Engine.Engine engine) : IHostedService {

  public async Task StartAsync(CancellationToken cancellationToken) {
    // By running the engine in a task but not awaiting it, we can return immediately.
    // This allows the host to continue running and not block while keeping the engine responsive.
    _ = Task.Run(engine.Run, cancellationToken);
    await Task.CompletedTask;
  }

  public async Task StopAsync(CancellationToken cancellationToken) {
    await engine.Cleanup();
  }
}