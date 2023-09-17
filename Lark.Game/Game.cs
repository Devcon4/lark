//An IHostedService that runs the engine and begins the game.

using Lark.Engine;
using Microsoft.Extensions.Hosting;

namespace Lark.Game;
public class Game(Lark.Engine.Engine engine) : IHostedService {
  public async Task StartAsync(CancellationToken cancellationToken) {
    await engine.Run();
  }

  public async Task StopAsync(CancellationToken cancellationToken) {
    await engine.Cleanup();
  }
}