using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Lark.Engine;
public class LarkWindow {

  public IWindow rawWindow = null!;
  private readonly ILogger<LarkWindow> _logger;
  private readonly IHostApplicationLifetime _hostLifetime;

  public LarkWindow(ILogger<LarkWindow> logger, IHostApplicationLifetime hostLifetime) {
    _logger = logger;
    _hostLifetime = hostLifetime;
  }

  public void Build(Action<IWindow> configure) {
    var options = WindowOptions.DefaultVulkan;
    options.Title = "Lark";
    rawWindow = Window.Create(options);
    rawWindow.Closing += onClosing;
    configure(rawWindow);
    rawWindow.Initialize();
  }

  private void onClosing() {
    // Need to shutdown the host when the window closes.
    _hostLifetime.StopApplication();
  }

  public void run() {
    _logger.LogInformation("Running window...");
    rawWindow.Run();
  }

  public void Cleanup() {
    _logger.LogInformation("Disposing window...");
    rawWindow?.Dispose();
  }
}