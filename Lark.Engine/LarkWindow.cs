using System.Diagnostics;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Lark.Engine.std;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Silk.NET.Core.Native;
using Silk.NET.GLFW;
using Silk.NET.Maths;

namespace Lark.Engine;

public class LarkWindow(ILogger<LarkWindow> logger, IOptions<GameSettings> options, ShutdownManager shutdownManager) {
  private readonly Glfw _glfw = Glfw.GetApi();
  public unsafe WindowHandle* windowHandle = null!;
  public Vector2 ViewportSize => new(FramebufferSize.X, FramebufferSize.Y);
  public unsafe bool IsFocused => _glfw.GetWindowAttrib(windowHandle, WindowAttributeGetter.Focused);

  private readonly GameSettings options = options.Value;

  public unsafe void Build() {
    logger.LogInformation("Building window... {thread}", Environment.CurrentManagedThreadId);
    _glfw.Init();

    // Set error callback.
    _glfw.SetErrorCallback((error, description) => {
      logger.LogError("GLFW Error ({error}): {description}", error, description);
    });

    _glfw.WindowHint(WindowHintClientApi.ClientApi, ClientApi.NoApi);
    _glfw.WindowHint(WindowHintBool.Resizable, false); // TODO: Fix this later, currently has a swapchain resize crash.
    _glfw.WindowHint(WindowHintBool.Focused, true);
    _glfw.WindowHint(WindowHintBool.Visible, true);
    _glfw.WindowHint(WindowHintBool.CenterCursor, true);
    _glfw.WindowHint(WindowHintBool.FocusOnShow, true);
    // Get the primary monitor.
    var monitor = options.Fullscreen ? _glfw.GetPrimaryMonitor() : null;
    // Get monitor resolution.
    var mode = options.Fullscreen ? _glfw.GetVideoMode(monitor) : null;
    var width = options.Fullscreen ? mode->Width : 800;
    var height = options.Fullscreen ? mode->Height : 600;

    windowHandle = _glfw.CreateWindow(width, height, "Lark", monitor, null);

    if (windowHandle == null) {
      throw new Exception("Failed to create GLFW window");
    }

    _glfw.SetWindowCloseCallback(windowHandle, (window) => {
      OnClosing();
    });
  }

  // TODO: check that vulkan follows this.
  public unsafe void SetVSync(bool vsync) {
    _glfw.SwapInterval(vsync ? 1 : 0);
  }

  // getter that returns the window time.
  public double Time => _glfw.GetTime();

  private void OnClosing() {
    // Need to shutdown the host when the window closes.
    shutdownManager.Exit();
  }

  public unsafe void CreateVkSurface(VkHandle instance, out VkNonDispatchableHandle surface) {
    fixed (VkNonDispatchableHandle* surfacePtr = &surface) {
      _glfw.CreateWindowSurface(instance, windowHandle, null, surfacePtr);
    }
  }

  public unsafe byte** GetRequiredInstanceExtensions(out uint count) {
    return _glfw.GetRequiredInstanceExtensions(out count);
  }

  public bool VulkanSupported() {
    return _glfw.VulkanSupported();
  }

  public unsafe void DoEvents() {
    // Do PollEvents using threadUtils main thread.

    // _glfw.WaitEvents();
    _glfw.PollEvents();
  }

  public unsafe bool ShouldClose() {
    return _glfw.WindowShouldClose(windowHandle);
  }

  public unsafe void Cleanup() {
    logger.LogInformation("Disposing window...");
    _glfw.DestroyWindow(windowHandle);
    _glfw.Terminate();
  }

  // public void Run(Action drawFrame) {
  //   logger.LogInformation("Running window...");
  //   while (!ShouldClose()) {
  //     DoEvents();
  //     drawFrame();
  //   }
  // }

  public unsafe void LogWindow() {
    logger.LogInformation("WindowHandle: {windowHandle}", (IntPtr)windowHandle);
  }

  public unsafe void SetFramebufferResize(Action<Vector2D<int>> framebufferResize) {
    _glfw.SetFramebufferSizeCallback(windowHandle, (window, width, height) => {
      framebufferResize(new Vector2D<int>(width, height));
    });
  }

  public unsafe Vector2D<int> FramebufferSize {
    get {
      _glfw.GetFramebufferSize(windowHandle, out var width, out var height);
      return new Vector2D<int>(width, height);
    }
  }

  public unsafe void SetCursorMode(CursorModeValue cursorMode, bool raw = true) {
    logger.LogInformation("Setting cursor mode to {cursorMode}", cursorMode);
    if (raw && cursorMode is not CursorModeValue.CursorDisabled) {
      throw new Exception("Raw mouse input is only supported when the cursor is disabled.");
    }

    if (raw && _glfw.RawMouseMotionSupported()) {
      _glfw.SetInputMode(windowHandle, CursorStateAttribute.RawMouseMotion, true);
    }

    _glfw.SetInputMode(windowHandle, CursorStateAttribute.Cursor, cursorMode);
  }

  public unsafe void SetKeyCallback(Action<Keys, int, InputAction, KeyModifiers> keyCallback) {
    _glfw.SetKeyCallback(windowHandle, (window, key, scancode, action, mods) => {
      // logger.LogInformation("Key: {key} :: {scancode} :: {action} :: {mods}", key, scancode, action, mods);
      keyCallback(key, scancode, action, mods);
    });
  }

  public unsafe void SetMouseButtonCallback(Action<MouseButton, InputAction, KeyModifiers> mouseButtonCallback) {
    _glfw.SetMouseButtonCallback(windowHandle, (window, button, action, mods) => {
      mouseButtonCallback(button, action, mods);
    });
  }

  public unsafe void SetCursorPosCallback(Action<Vector2> cursorPosCallback) {
    _glfw.SetCursorPosCallback(windowHandle, (window, x, y) => {
      cursorPosCallback(new Vector2((float)x, (float)y));
    });
  }

  public unsafe void SetScrollCallback(Action<Vector2> scrollCallback) {
    _glfw.SetScrollCallback(windowHandle, (window, x, y) => {
      scrollCallback(new Vector2((float)x, (float)y));
    });
  }

  internal unsafe void SetCursorPosition(Vector2? position) {
    if (position is null) {
      // If null set to center of window
      position = new Vector2(FramebufferSize.X / 2, FramebufferSize.Y / 2);
    }
    _glfw.SetCursorPos(windowHandle, (float)position?.X, (float)position?.Y);
  }

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