using System.Numerics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silk.NET.Core.Native;
using Silk.NET.GLFW;
using Silk.NET.Maths;

namespace Lark.Engine;

public class LarkWindow(ILogger<LarkWindow> logger, IHostApplicationLifetime hostLifetime) {
  private readonly Glfw _glfw = Glfw.GetApi();
  public unsafe WindowHandle* windowHandle = null!;
  public unsafe void Build() {
    _glfw.Init();

    // Set error callback.
    _glfw.SetErrorCallback((error, description) => {
      logger.LogError("GLFW Error ({error}): {description}", error, description);
    });

    _glfw.WindowHint(WindowHintClientApi.ClientApi, ClientApi.NoApi);
    _glfw.WindowHint(WindowHintBool.Resizable, true);

    windowHandle = _glfw.CreateWindow(800, 600, "Lark", null, null);

    if (windowHandle == null) {
      throw new Exception("Failed to create GLFW window");
    }

    _glfw.SetWindowCloseCallback(windowHandle, (window) => {
      OnClosing();
    });
  }

  // getter that returns the window time.
  public double Time => _glfw.GetTime();

  private void OnClosing() {
    // Need to shutdown the host when the window closes.
    hostLifetime.StopApplication();
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
    _glfw.PollEvents();
  }

  private unsafe bool ShouldClose() {
    return _glfw.WindowShouldClose(windowHandle);
  }

  public unsafe void Cleanup() {
    logger.LogInformation("Disposing window...");
    _glfw.DestroyWindow(windowHandle);
    _glfw.Terminate();
  }

  public void Run(Action drawFrame) {
    logger.LogInformation("Running window...");
    while (!ShouldClose()) {
      _glfw.PollEvents();
      drawFrame();
    }
  }

  public async Task Run(Func<Task> drawFrame) {
    logger.LogInformation("Running window...");
    while (!ShouldClose()) {
      _glfw.PollEvents();
      await drawFrame();
    }
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

  public unsafe void SetKeyCallback(Action<Keys, int, InputAction, KeyModifiers> keyCallback) {
    _glfw.SetKeyCallback(windowHandle, (window, key, scancode, action, mods) => {
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