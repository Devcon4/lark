using System.Text;
using System.Text.Json;
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

public unsafe class UltralightController(ILogger<UltralightController> logger, TimeManager tm, UltralightStatus status, IHostEnvironment env) {
  private Config* _config;
  private Renderer* _renderer;
  private Session* _session;
  public View* View;
  // private View* _inspectorView;
  private static readonly Guid _instanceId = Guid.NewGuid();
  private readonly string InstanceName = $"Lark.Engine.Ultralight.{_instanceId}";

  private void OnLogMessage(LogLevel level, string message) => Console.WriteLine($"{level}: " + message);

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
    var resourcesPath = ultralightString.Create(Path.Combine(env.ContentRootPath, "ultralight"));
    _config->SetResourcePath(resourcesPath);
    resourcesPath->Destroy();

    _config->SetUseGpuRenderer(false);
    _config->SetEnableImages(true);
    _config->SetEnableJavaScript(true);
    // _config->SetUserStylesheet(ultralightString.Create("h1 { color: cyan; } body { background: transparent; }"));

    AppCore.EnablePlatformFontLoader();

    // setup the asset path
    var assetPath = ultralightString.Create(Path.Combine(env.ContentRootPath, "resources", "client"));
    AppCore.EnablePlatformFileSystem(assetPath);
    assetPath->Destroy();

    _renderer = Renderer.Create(_config);
    var sessionName = ultralightString.Create("Lark.Engine.Ultralight");
    _session = Session.Create(_renderer, false, sessionName);

    // todo match the view size to the window size
    View = ImpromptuNinjas.UltralightSharp.View.Create(_renderer, 800, 600, true, _session);
    logger.LogInformation("Ultralight initialized");

    // _view->LoadHtml(htmlString);
    // htmlString->Destroy();

    View->SetAddConsoleMessageCallback(AddConsoleMessageCallback(), null);
    View->SetFinishLoadingCallback(FinishLoadingCallback(), null);

    // Create inspector view
    // _inspectorView = _view->CreateInspector();
    // var inspectorString = ultralightString.Create($"file:///inspector/Main.html");
    // _inspectorView->LoadUrl(inspectorString);
    // inspectorString->Destroy();

    // Todo: We should prob navigate to index.html here.

    var urlString = ultralightString.Create($"file:///index.html");
    View->LoadUrl(urlString);
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
    var state = new { fps = tm.FPS };
    SetState(ToJson(state));

  }

  public void Cleanup() {
    View->Destroy();
    _session->Destroy();
    _renderer->Destroy();
    _config->Destroy();

    // cleanup the temp directory
    var tempDir = Path.GetTempPath();
    Directory.Delete(Path.Combine(tempDir, InstanceName), true);
  }

  private JsString* FromString(string str) {
    var strBytes = Encoding.UTF8.GetBytes(str);
    var strMem = new ReadOnlyMemory<byte>(strBytes);
    var jsString = JavaScriptCore.StringCreateWithUtf8CString((sbyte*)strMem.Pin().Pointer);
    return jsString;
  }

  public static string ToJson<T>(T obj) => JsonSerializer.Serialize(obj);
  public unsafe HashSet<string> ListObjectProperties(JsValue* jsObject) {
    var ctx = View->LockJsContext();

    // Get the property names
    JsPropertyNameArray* propertyNames = JavaScriptCore.JsObjectCopyPropertyNames(ctx, jsObject);
    var propertyLen = JavaScriptCore.PropertyNameArrayGetCount(propertyNames);

    var props = new HashSet<string>();
    // Iterate over the properties
    for (uint i = 0; i < propertyLen; i++) {
      // Get the property name
      var propertyName = JavaScriptCore.PropertyNameArrayGetNameAtIndex(propertyNames, i);
      // Convert the property name to a C# string
      var propertyNameString = ToString(propertyName);
      props.Add(propertyNameString);
    }

    return props;
  }
  public unsafe string ToString(JsString* jsString) {
    // Get the length of the JSString in bytes
    var length = JavaScriptCore.StringGetMaximumUtf8CStringSize(jsString);

    // Allocate a byte array to hold the UTF-8 string
    var bytes = new byte[length];

    // Get a pointer to the byte array
    fixed (byte* bytesPtr = bytes) {
      // Convert the JSString to a UTF-8 string
      JavaScriptCore.StringGetUtf8CString(jsString, (sbyte*)bytesPtr, length);
    }

    // Convert the byte array to a C# string
    var str = Encoding.UTF8.GetString(bytes);

    return str;
  }
  public void SetState(string stateJson) {
    // logger.LogInformation("C# Setting state: {stateJson}", stateJson);
    var ctx = View->LockJsContext();
    var globalObject = JavaScriptCore.ContextGetGlobalObject(ctx);
    // var globalProps = ListObjectProperties(globalObject);

    // // Get the window object from the global object
    // var windowPropName = FromString("window");
    // var windowObject = JavaScriptCore.JsObjectGetProperty(ctx, globalObject, windowPropName, null);
    // var windowProps = ListObjectProperties(windowObject);

    // var stateString = FromString(stateJson);
    // var propString = FromString("state");

    // var stateValue = JavaScriptCore.JsValueMakeFromJsonString(ctx, stateString);
    // JavaScriptCore.JsObjectSetProperty(ctx, globalObject, propString, stateValue, ImpromptuNinjas.UltralightSharp.Enums.JsPropertyAttribute.ReadOnly, null);
    // var str = ultralightString.Create($"window.SetState(\"{stateJson}\")");
    // var str = ultralightString.Create($"console.log('script!');");
    var str = ultralightString.Create("window.SetState('{\"fps\": 100}');");
    ultralightString** ex = null;
    var rawRes = View->EvaluateScript(str, ex);
    var res = rawRes->Read();

    if (ex is not null) {
      logger.LogError("Error evaluating script: {error}", (*ex)->Read());
    }

    // Get the setState function
    // var setStatePropName = FromString("SetState");
    // var setStateFunc = JavaScriptCore.JsObjectGetProperty(ctx, globalObject, setStatePropName, null);

    // if (setStateFunc == null) {
    //   logger.LogError("Could not find SetState function");
    //   return;
    // }

    // Convert the stateJson string to a JSString
    // var stateString = FromString(stateJson);

    // Convert the JSString to a JSValue
    // var stateValue = JavaScriptCore.JsValueMakeString(ctx, stateString);

    // Call the setState function with the stateValue as argument
    // JsValue*[] args = { stateValue };
    // fixed (JsValue** argPtr = args) {
    //   JavaScriptCore.JsObjectCallAsFunction(ctx, setStateFunc, globalObject, (uint)args.Length, argPtr, null);
    // }
  }

  public FinishLoadingCallback FinishLoadingCallback() {
    return (view, caller, frameId, isMainFrame, url) => {
      var htmlStr = ultralightString.Create("document.documentElement.outerHTML");
      var jsResult = View->EvaluateScript(htmlStr, null);
      // var jsResult = _view->EvaluateScript(ultralightString.Create("document.documentElement.outerHTML"), null)->Read();

      var html = jsResult->Read();
      logger.LogInformation("Ultralight Loaded page: {html}", html ?? "null");

      var str = ultralightString.Create("window.SetState('{\"fps\": 100}');");
      ultralightString** ex = null;
      View->EvaluateScript(str, ex);
    };
  }

  public AddConsoleMessageCallback AddConsoleMessageCallback() {
    return (view, caller, source, level, message, line, col, src) => {
      logger.LogInformation("Ultralight console message: {Message}", message->Read() ?? "null");
    };
  }

  public void SetViewport(uint width, uint height) {
    View->Resize(width, height);
  }

  public Bitmap* GetBitmap() {
    var surface = View->GetSurface();
    return surface->GetBitmap();
  }
}
