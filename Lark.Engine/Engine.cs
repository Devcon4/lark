// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Silk.NET.Windowing;

namespace Lark.Engine;

public partial class Engine {
  private readonly LarkWindow _larkWindow;
  private readonly VulkanBuilder _vulkanBuilder;
  private readonly ILogger<Engine> _logger;

  public Engine(LarkWindow larkWindow, VulkanBuilder vulkanBuilder, ILogger<Engine> logger) {
    _larkWindow = larkWindow;
    _vulkanBuilder = vulkanBuilder;
    _logger = logger;
  }

  public async Task run() {
    _logger.LogInformation("Running engine...");
    _larkWindow.Build(window => {
      window.Load += OnLoad;
      window.Update += OnUpdate;
      window.Render += OnRender;
    });

    await Init();

    _ = Task.Run(() => _larkWindow.run());
  }

  private async Task Init() {
    _logger.LogInformation("Initializing engine...");
    _vulkanBuilder.InitVulkan();
    await Task.CompletedTask;
  }

  private void OnRender(double obj) { }

  private void OnUpdate(double obj) {
    _logger.LogDebug("Update");
  }

  private void OnLoad() {
    _logger.LogInformation("Window loaded.");
  }

  public async Task Dispose() {
    _logger.LogInformation("Disposing engine...");
    _ = Task.Run(() => _larkWindow.Dispose());
    await Task.CompletedTask;
  }
}