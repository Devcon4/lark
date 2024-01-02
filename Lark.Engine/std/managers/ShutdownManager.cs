using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lark.Engine.std;

public class ShutdownManager(ILogger<ShutdownManager> logger, IHostApplicationLifetime applicationLifetime) {
  public void Exit() {
    logger.LogInformation("Application exit requested.");
    applicationLifetime.ApplicationStopped.Register(() => {
      logger.LogInformation("Application exit complete.");
    });
    applicationLifetime.StopApplication();
  }
}