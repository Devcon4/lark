using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Lark.Engine;
public class LarkWindow {

  private IWindow window = null!;
  private readonly ILogger<LarkWindow> _logger;
  private readonly IHostApplicationLifetime _hostLifetime;

  public LarkWindow(ILogger<LarkWindow> logger, IHostApplicationLifetime hostLifetime) {
    _logger = logger;
    _hostLifetime = hostLifetime;
  }

  public void Build(Action<IWindow> configure) {
    var options = WindowOptions.DefaultVulkan;
    options.Size = new Vector2D<int>(800, 600);
    options.Title = "Lark";
    window = Window.Create(options);
    window.Closing += onClosing;
    configure(window);
  }

  private void onClosing() {
    // Need to shutdown the host when the window closes.
    _hostLifetime.StopApplication();
  }

  public void run() {
    _logger.LogInformation("Running window...");
    window.Run();
  }

  public void Dispose() {
    _logger.LogInformation("Disposing window...");
    window?.Dispose();
  }
}