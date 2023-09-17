using Microsoft.Extensions.Logging;
using Silk.NET.Maths;

namespace Lark.Engine;

public partial class Engine {
  private readonly LarkWindow _larkWindow;
  private readonly VulkanBuilder _vulkanBuilder;
  private readonly ShaderBuilder _shaderBuilder;
  private readonly ILogger<Engine> _logger;
  private bool _framebufferResized = false;

  public Engine(LarkWindow larkWindow, VulkanBuilder vulkanBuilder, ShaderBuilder shaderBuilder, ILogger<Engine> logger) {
    _larkWindow = larkWindow;
    _vulkanBuilder = vulkanBuilder;
    _shaderBuilder = shaderBuilder;
    _logger = logger;
  }

  public async Task run() {
    _logger.LogInformation("Running engine...");
    _larkWindow.Build(window => {
      window.Load += OnLoad;
      window.Update += OnUpdate;
      window.Render += _vulkanBuilder.DrawFrame;
    });

    await Init();

    _ = Task.Run(() => {
      _larkWindow.run();
      _vulkanBuilder.Wait();
    });
  }

  private async Task Init() {
    _logger.LogInformation("Initializing engine...");
    _logger.LogInformation("Window: {Window}", _larkWindow.rawWindow);
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

  public async Task Cleanup() {
    _logger.LogInformation("Disposing engine...");
    _ = Task.Run(() => {
      _vulkanBuilder.Cleanup();
      _larkWindow.Cleanup();
    });
    await Task.CompletedTask;
  }
}