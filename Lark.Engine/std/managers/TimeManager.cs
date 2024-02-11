using Lark.Engine.ecs;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Lark.Engine.std;

public class TimeManager(ILogger<TimeManager> logger) : LarkManager {
  public TimeSpan DeltaTime { get; private set; }
  public TimeSpan TotalTime { get; private set; }
  public int FPS { get; private set; } = 0;
  public int TotalFrames { get; private set; } = 0;

  public float HighestFrameTime { get; private set; } = 0;
  public float LowestFrameTime { get; private set; } = 100;

  private DateTime lastFrame = DateTime.Now;
  private TimeSpan FpsTime { get; set; } = TimeSpan.Zero;
  private int FrameCount { get; set; } = 0;
  private int LastSecond { get; set; } = 0;

  private IDisposable frameContext;
  private IDisposable fpsContext;

  public void Update() {
    var now = DateTime.Now;
    DeltaTime = now - lastFrame;
    TotalTime += DeltaTime;
    FpsTime += DeltaTime;

    FrameCount++;
    TotalFrames++;

    frameContext?.Dispose();
    frameContext = LogContext.PushProperty("Frame", FrameCount);

    if (FpsTime.TotalSeconds >= 1) {
      FPS = FrameCount;
      FrameCount = 0;
      FpsTime = TimeSpan.Zero;
    }

    fpsContext?.Dispose();
    fpsContext = LogContext.PushProperty("FPS", FPS);

    if (DeltaTime.TotalMilliseconds > HighestFrameTime) {
      HighestFrameTime = (float)DeltaTime.TotalMilliseconds;
    }

    if (DeltaTime.TotalMilliseconds < LowestFrameTime) {
      LowestFrameTime = (float)DeltaTime.TotalMilliseconds;
    }

    lastFrame = now;

    if (TotalTime.Seconds != LastSecond) {
      LastSecond = TotalTime.Seconds;
      logger.LogInformation("FPS: {fps}, T: {lastsecond}", FPS, TotalTime.ToString(@"mm\:ss"));
    }

    logger.LogDebug("{Frame} \t:: Δ {deltaTime}ms \t:: {fps} \t:: High {highest} \t:: Low {low}", TotalFrames, DeltaTime.TotalMilliseconds, FPS, HighestFrameTime, LowestFrameTime);
  }
}
