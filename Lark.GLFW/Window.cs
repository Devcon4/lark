
using Microsoft.Extensions.Hosting;
using Silk.NET.GLFW;
using System.Diagnostics;

public class Engine(Window window) {
  public Task Run() {
    window.Build();

    window.Run(GameLoop);
    return Task.CompletedTask;
  }

  private int frame = 0;
  public void GameLoop() {
    var _ = Task.Run(() => {
      Console.WriteLine($"Frame {frame++}");
    });
  }
}

public class Game(Engine engine) : IHostedService {
  public async Task StartAsync(CancellationToken cancellationToken) {
    var _ = Task.Run(engine.Run, cancellationToken);
    await Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken) {
    return Task.CompletedTask;
  }
}

public class Window {
  private readonly Glfw glfw = Glfw.GetApi();
  private unsafe WindowHandle* window;
  public unsafe bool IsFocused => glfw.GetWindowAttrib(window, WindowAttributeGetter.Focused);

  public unsafe void Build() {
    Console.WriteLine("Hello, World!");

    glfw.Init();
    glfw.SetErrorCallback((error, description) => {
      Console.WriteLine($"GLFW Error {error}: {description}");
    });

    window = glfw.CreateWindow(640, 480, "Hello World", null, null);

    glfw.SetKeyCallback(window, (window, key, scancode, action, mods) => {
      Console.WriteLine($"Key: {key}, Scancode: {scancode}, Action: {action}, Mods: {mods}");
    });
  }

  private unsafe bool ShouldClose() {
    return glfw.WindowShouldClose(window);
  }

  public void Run(Action loop) {
    var frameSW = new Stopwatch();
    var spinSW = new Stopwatch();

    while (!ShouldClose()) {
      var fps = 30;
      var targetTime = 1000f / fps;
      frameSW.Restart();

      glfw.PollEvents();
      loop();
      // Console.WriteLine("Hello, World!");
      // Console.WriteLine($"IsFocused: {IsFocused}");
      Console.WriteLine($"IsFocused: {IsFocused} :: Current Thread: {Environment.CurrentManagedThreadId}");


      double frameTime = frameSW.Elapsed.TotalMilliseconds;
      if (frameTime < targetTime) {
        double sleepTime = targetTime - frameTime;
        spinSW.Restart();
        while (spinSW.Elapsed.TotalMilliseconds < sleepTime) { }
      }
    }
  }
}