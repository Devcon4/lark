using Evergine.Bindings.RenderDoc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lark.Engine;

public class RenderDocOptions {
  public bool Enabled { get; set; } = false;
}

public class LarkRenderDocModule(IOptions<RenderDocOptions> options, ILogger<LarkRenderDocModule> logger) : ILarkModule {
  public RenderDoc? renderDoc = null;


  public Task Init() {
    if (options.Value.Enabled) {
      logger.LogInformation("Loading RenderDoc...");
      RenderDoc.Load(out renderDoc);
    }

    return Task.CompletedTask;
  }

  public Task Run() {
    return Task.CompletedTask;
  }

  public Task Cleanup() {
    return Task.CompletedTask;
  }
}