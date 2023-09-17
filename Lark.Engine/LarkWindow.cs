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

  public unsafe void Cleanup() {
    logger.LogInformation("Disposing window...");
    _glfw.DestroyWindow(windowHandle);
    _glfw.Terminate();
  }

  public unsafe void Run(Action drawFrame) {
    logger.LogInformation("Running window...");
    while (!_glfw.WindowShouldClose(windowHandle)) {
      _glfw.PollEvents();
      drawFrame();
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