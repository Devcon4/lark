using ImpromptuNinjas.UltralightSharp;
using Lark.Engine.std;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ultralightString = ImpromptuNinjas.UltralightSharp.String;

namespace Lark.Engine.Ultralight;

public class UltralightStatus {
  public TaskCompletionSource Initialized { get; } = new TaskCompletionSource();
}

public unsafe class UltralightController(ILogger<UltralightController> logger, IOptions<UltralightConfig> options, TimeManager tm, UltralightStatus status, IHostEnvironment env) {
  private Config* _config;
  private Renderer* _renderer;
  private Session* _session;
  private View* _view;

  private static readonly Guid _instanceId = Guid.NewGuid();
  private readonly string InstanceName = $"Lark.Engine.Ultralight.{_instanceId}";

  private unsafe void OnLogMessage(LogLevel level, string message) => logger.Log(level, message);

  public Task StartAsync() {
    logger.LogInformation("Initializing Ultralight... instance: {InstanceName}", InstanceName);

    Dictionary<ImpromptuNinjas.UltralightSharp.Enums.LogLevel, LogLevel> ultralightLogLevelToLogLevel = new() {
      { ImpromptuNinjas.UltralightSharp.Enums.LogLevel.Info, LogLevel.Information },
      { ImpromptuNinjas.UltralightSharp.Enums.LogLevel.Warning, LogLevel.Warning },
      { ImpromptuNinjas.UltralightSharp.Enums.LogLevel.Error, LogLevel.Error }
    };

    LoggerLogMessageCallback logMessageCallback = (level, message) => OnLogMessage(ultralightLogLevelToLogLevel[level], message->Read() ?? string.Empty);
    ImpromptuNinjas.UltralightSharp.Ultralight.SetLogger(new Logger { LogMessage = logMessageCallback });

    _config = Config.Create();

    var tempDir = Path.GetTempPath();

    Directory.CreateDirectory(Path.Combine(tempDir, InstanceName));

    var cachePath = ultralightString.Create(Path.Combine(tempDir, InstanceName, "cache"));
    _config->SetCachePath(cachePath);
    cachePath->Destroy();

    // resources are in client/resources folder in the bin. Setup the resources path.
    var resourcesPath = ultralightString.Create(Path.Combine(env.ContentRootPath, "client", "resources"));
    _config->SetResourcePath(resourcesPath);
    resourcesPath->Destroy();

    _config->SetUseGpuRenderer(false);
    _config->SetEnableImages(true);
    _config->SetEnableJavaScript(false);

    AppCore.EnablePlatformFontLoader();

    // setup the asset path
    var assetPath = ultralightString.Create(Path.Combine(env.ContentRootPath, "client", "assets"));
    AppCore.EnablePlatformFileSystem(assetPath);
    assetPath->Destroy();

    _renderer = Renderer.Create(_config);
    var sessionName = ultralightString.Create("Lark.Engine.Ultralight");
    _session = Session.Create(_renderer, false, sessionName);

    // todo match the view size to the window size
    _view = View.Create(_renderer, 800, 600, true, _session);

    logger.LogInformation("Ultralight initialized");

    var htmlString = ultralightString.Create("<html><body><h1>Hello, Ultralight!</h1></body></html>");
    _view->LoadHtml(htmlString);
    htmlString->Destroy();

    _view->SetFinishLoadingCallback(FinishLoadingCallback(), null);

    // Todo: We should prob navigate to index.html here.
    var urlString = ultralightString.Create("file:///index.html");
    _view->LoadUrl(urlString);
    urlString->Destroy();


    status.Initialized.SetResult();
    return Task.CompletedTask;
  }

  public void Update() {
    // Investigate: Why does ultralight have two update/render methods?
    // ImpromptuNinjas.UltralightSharp.Ultralight.Update(_renderer);
    // ImpromptuNinjas.UltralightSharp.Ultralight.Render(_renderer);
    _renderer->Update();
    _renderer->Render();
  }

  public void Cleanup() {
    _view->Destroy();
    _session->Destroy();
    _renderer->Destroy();
    _config->Destroy();

    // cleanup the temp directory
    var tempDir = Path.GetTempPath();
    Directory.Delete(Path.Combine(tempDir, InstanceName), true);
  }

  public FinishLoadingCallback FinishLoadingCallback() {
    return (view, caller, frameId, isMainFrame, url) => {
      logger.LogInformation("Ultralight finished loading url: {Url}", url->Read() ?? "null");
    };
  }
}
