//An IHostedService that runs the engine and begins the game.

using Lark.Engine;
using Microsoft.Extensions.Hosting;

public class Game : IHostedService {
  private readonly Engine engine;

  public Game(Engine engine) {
    this.engine = engine;
  }

  public async Task StartAsync(CancellationToken cancellationToken) {
    await engine.run();
  }

  public async Task StopAsync(CancellationToken cancellationToken) {
    await engine.Cleanup();
  }
}