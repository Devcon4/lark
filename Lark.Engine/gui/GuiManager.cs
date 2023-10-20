using CefSharp;
using CefSharp.OffScreen;
using Lark.Engine.Model;
using Lark.Engine.Pipeline;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace Lark.Engine.gui;

public class GUIData {
  public ChromiumWebBrowser browser;

  public LarkImage[] guiImages;
  public DeviceMemory guiImageMemory;
}

public class GuiManager(GUIData guiData, LarkVulkanData data, LarkWindow window, ImageUtils imageUtils) {

  CefSettings settings = new() { };
  const string testUrl = "https://www.google.com/";

  public async Task Init() {
    var success = await Cef.InitializeAsync(settings, performDependencyCheck: true, browserProcessHandler: null);

    if (!success) {
      throw new Exception("Unable to Initialize Cef");
    }

    guiData.browser = new ChromiumWebBrowser(testUrl);
    var initRes = await guiData.browser.WaitForInitialLoadAsync();

    if (!initRes.Success) {
      throw new Exception("Unable to load " + testUrl);
    }

    CreateGuiImages((uint)window.FramebufferSize.X, (uint)window.FramebufferSize.Y);

    guiData.browser.Paint += OnBrowserPaint;
  }

  public void Resize(Vector2D<int> size) {
    foreach (var image in guiData.guiImages) {
      image.Dispose(data);
    }
    CreateGuiImages((uint)size.X, (uint)size.Y);
  }

  public void CreateGuiImages(uint width, uint height) {
    guiData.guiImages = new LarkImage[LarkVulkanData.MaxFramesInFlight];

    for (int i = 0; i < LarkVulkanData.MaxFramesInFlight; i++) {
      imageUtils.CreateImage(
        width,
        height,
        Format.R8G8B8A8Unorm,
        ImageTiling.Linear,
        ImageUsageFlags.TransferDstBit,
        MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
        ref guiData.guiImages[i].Image,
        ref guiData.guiImages[i].Memory);

      imageUtils.CreateImageView(guiData.guiImages[i].Image, ref guiData.guiImages[i].View);
      imageUtils.CreateSampler(ref guiData.guiImages[i].Sampler);
    }
  }

  public unsafe void OnBrowserPaint(object? sender, OnPaintEventArgs e) {

    var width = e.Width;
    var height = e.Height;
    var size = (uint)(width * height * 4);

    var span = new ReadOnlySpan<byte>(e.BufferHandle.ToPointer(), (int)size);
    for (int i = 0; i < LarkVulkanData.MaxFramesInFlight; i++) {
      imageUtils.UpdateTexture(span, ref guiData.guiImages[i].Image);

    }
  }


  public void Cleanup() {
    guiData.browser.Dispose();
    Cef.Shutdown();
  }
}