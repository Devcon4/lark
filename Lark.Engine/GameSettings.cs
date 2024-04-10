namespace Lark.Engine;

public class GameSettings {
  public bool VSync { get; set; } = false;
  public int? FPSLimit { get; set; } = null;
  public float MouseSensitivity { get; set; } = 1f;

  public bool Fullscreen { get; set; } = false;
}

// public class LarkWindow(ILogger<LarkWindow> logger, IHostApplicationLifetime hostLifetime) {

//   public IWindow rawWindow = null!;

//   public void Build(Action<IWindow> configure) {
//     var options = WindowOptions.DefaultVulkan;
//     options.Title = "Lark";
//     rawWindow = Window.Create(options);
//     rawWindow.Closing += OnClosing;
//     configure(rawWindow);
//     rawWindow.Initialize();
//   }

//   private void OnClosing() {
//     // Need to shutdown the host when the window closes.
//     hostLifetime.StopApplication();
//   }

//   public void Run() {
//     logger.LogInformation("Running window...");
//     rawWindow.Run();
//   }

//   public void Cleanup() {
//     logger.LogInformation("Disposing window...");
//     rawWindow?.Dispose();
//   }
// }