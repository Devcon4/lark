using System.Diagnostics;
using Lark.Engine.ecs;
using Lark.Engine.physx.managers;
using Lark.Engine.pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Silk.NET.SDL;

namespace Lark.Engine;

public class EcsModule(SystemManager systemManager) : ILarkModule {
  public Task Cleanup() {
    return Task.CompletedTask;
  }

  public Task Init() {
    systemManager.Init();
    return Task.CompletedTask;
  }

  public async Task Run() {
    await systemManager.Run();
  }
}

public partial class Engine(LarkWindow larkWindow, IEnumerable<ILarkModule> modules, ILogger<Engine> logger, IOptionsMonitor<GameSettings> gameSettings, IHostApplicationLifetime hostLifetime) {
  public void Run(CancellationToken cancellationToken) {
    logger.LogInformation("Running engine... {thread}", Environment.CurrentManagedThreadId);
    larkWindow.Build();

    Init();
    GameLoop(cancellationToken);
  }

  public void GameLoop(CancellationToken cancellationToken) {
    var frameSW = new Stopwatch();
    var spinSW = new Stopwatch();

    logger.LogInformation("FPS Limit: {fps}", gameSettings.CurrentValue.FPSLimit);

    while (!larkWindow.ShouldClose() && !cancellationToken.IsCancellationRequested) {
      var targetTime = 1000f / gameSettings.CurrentValue.FPSLimit.GetValueOrDefault(60);
      frameSW.Restart();

      larkWindow.DoEvents();
      foreach (var module in modules) {
        module.Run().Wait();
      }

      double frameTime = frameSW.Elapsed.TotalMilliseconds;
      if (gameSettings.CurrentValue.FPSLimit.HasValue && frameTime < targetTime) {
        double sleepTime = targetTime - frameTime;

        // Task.delay is not accurate enough, so we need to use a spin wait.
        spinSW.Restart();
        while (spinSW.Elapsed.TotalMilliseconds < sleepTime) { }
      }
    }

  }

  private void Init() {
    logger.LogInformation("Initializing engine...");

    foreach (var module in modules) {
      logger.LogInformation("Initializing module {module}", module.GetType().Name);
      module.Init().Wait();
    }
  }

  public void Cleanup() {
    logger.LogInformation("Disposing engine... {thread}", Environment.CurrentManagedThreadId);
    foreach (var module in modules) {
      logger.LogInformation("Disposing module {module}", module.GetType().Name);
      module.Cleanup();
    }

    larkWindow.Cleanup();
  }
}

public static class HostExtensions {
  public static void RunEngine(this IHost host) {
    var engine = host.Services.GetRequiredService<Engine>();
    var hostLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

    var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger<Engine>();

    var tokenSrc = new CancellationTokenSource();
    var token = tokenSrc.Token;

    hostLifetime.ApplicationStopping.Register(() => {
      tokenSrc.Cancel();
    });

    try {

      host.StartAsync(token).ConfigureAwait(continueOnCapturedContext: false);
      engine.Run(token);
    }
    finally {
      engine.Cleanup();
      if (host is IAsyncDisposable asyncDisposable) {
        asyncDisposable.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
      }
      else {
        host.Dispose();
      }
    }
  }
}