using Microsoft.Extensions.Logging;

namespace Lark.Engine;

public class VulkanBuilder {
  private readonly ILogger<VulkanBuilder> _logger;

  public VulkanBuilder(ILogger<VulkanBuilder> logger) {
    _logger = logger;
  }

  public void InitVulkan() {
    _logger.LogInformation("Initializing Vulkan...");
  }
}