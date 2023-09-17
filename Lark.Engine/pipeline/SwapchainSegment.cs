using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace Lark.Engine.Pipeline;

public class SwapchainSegment(LarkVulkanData data,
    LarkWindow larkWindow,
    SwapchainSupportUtil swapchainSupportUtil,
    QueueFamilyUtil queueFamilyUtil,
    ImageViewSegment imageViewSegment,
    RenderPassSegment renderPassSegment,
    GraphicsPipelineSegment graphicsPipelineSegment,
    FramebufferSegment framebufferSegment,
    CommandBufferSegment commandBufferSegment
    ) {
  public unsafe void RecreateSwapChain() {
    Vector2D<int> framebufferSize = larkWindow.rawWindow.FramebufferSize;

    while (framebufferSize.X == 0 || framebufferSize.Y == 0) {
      framebufferSize = larkWindow.rawWindow.FramebufferSize;
      larkWindow.rawWindow.DoEvents();
    }

    _ = data.vk.DeviceWaitIdle(data.Device);

    CleanupSwapchain();

    // TODO: On SDL it is possible to get an invalid swap chain when the window is minimized.
    // This check can be removed when the above frameBufferSize check catches it.
    while (!CreateSwapChain()) {
      larkWindow.rawWindow.DoEvents();
    }

    CreateSwapChain();
    imageViewSegment.CreateImageViews();
    renderPassSegment.CreateRenderPass();
    graphicsPipelineSegment.CreateGraphicsPipeline();
    framebufferSegment.CreateFramebuffers();
    commandBufferSegment.CreateCommandBuffers();

    data.ImagesInFlight = new Fence[data.SwapchainImages.Length];
  }


  public unsafe bool CreateSwapChain() {
    var swapChainSupport = swapchainSupportUtil.QuerySwapChainSupport(data.PhysicalDevice);

    var surfaceFormat = ChooseSwapSurfaceFormat(swapChainSupport.Formats);
    var presentMode = ChooseSwapPresentMode(swapChainSupport.PresentModes);
    var extent = ChooseSwapExtent(swapChainSupport.Capabilities);

    // TODO: On SDL minimizing the window does not affect the frameBufferSize.
    // This check can be removed if it does
    if (extent.Width == 0 || extent.Height == 0)
      return false;

    var imageCount = swapChainSupport.Capabilities.MinImageCount + 1;
    if (swapChainSupport.Capabilities.MaxImageCount > 0 &&
        imageCount > swapChainSupport.Capabilities.MaxImageCount) {
      imageCount = swapChainSupport.Capabilities.MaxImageCount;
    }

    var createInfo = new SwapchainCreateInfoKHR {
      SType = StructureType.SwapchainCreateInfoKhr,
      Surface = data.Surface,
      MinImageCount = imageCount,
      ImageFormat = surfaceFormat.Format,
      ImageColorSpace = surfaceFormat.ColorSpace,
      ImageExtent = extent,
      ImageArrayLayers = 1,
      ImageUsage = ImageUsageFlags.ColorAttachmentBit
    };

    var indices = queueFamilyUtil.FindQueueFamilies(data.PhysicalDevice);
    uint[] queueFamilyIndices = { indices.GraphicsFamily.Value, indices.PresentFamily.Value };

    fixed (uint* qfiPtr = queueFamilyIndices) {
      if (indices.GraphicsFamily != indices.PresentFamily) {
        createInfo.ImageSharingMode = SharingMode.Concurrent;
        createInfo.QueueFamilyIndexCount = 2;
        createInfo.PQueueFamilyIndices = qfiPtr;
      }
      else {
        createInfo.ImageSharingMode = SharingMode.Exclusive;
      }

      createInfo.PreTransform = swapChainSupport.Capabilities.CurrentTransform;
      createInfo.CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr;
      createInfo.PresentMode = presentMode;
      createInfo.Clipped = Vk.True;

      createInfo.OldSwapchain = default;

      if (!data.vk.TryGetDeviceExtension(data.Instance, data.vk.CurrentDevice.Value, out data.VkSwapchain)) {
        throw new NotSupportedException("KHR_data.Swapchain extension not found.");
      }

      fixed (SwapchainKHR* swapchain = &data.Swapchain) {
        if (data.VkSwapchain.CreateSwapchain(data.Device, &createInfo, null, swapchain) != Result.Success) {
          throw new Exception("failed to create swap chain!");
        }
      }
    }

    data.VkSwapchain.GetSwapchainImages(data.Device, data.Swapchain, &imageCount, null);
    data.SwapchainImages = new Image[imageCount];
    fixed (Image* swapchainImage = data.SwapchainImages) {
      data.VkSwapchain.GetSwapchainImages(data.Device, data.Swapchain, &imageCount, swapchainImage);
    }

    data.SwapchainImageFormat = surfaceFormat.Format;
    data.SwapchainExtent = extent;

    return true;
  }

  public unsafe void CleanupSwapchain() {
    foreach (var framebuffer in data.SwapchainFramebuffers) {
      data.vk.DestroyFramebuffer(data.Device, framebuffer, null);
    }

    fixed (CommandBuffer* buffers = data.CommandBuffers) {
      data.vk.FreeCommandBuffers(data.Device, data.CommandPool, (uint)data.CommandBuffers.Length, buffers);
    }

    data.vk.DestroyPipeline(data.Device, data.GraphicsPipeline, null);
    data.vk.DestroyPipelineLayout(data.Device, data.PipelineLayout, null);
    data.vk.DestroyRenderPass(data.Device, data.RenderPass, null);

    foreach (var imageView in data.SwapchainImageViews) {
      data.vk.DestroyImageView(data.Device, imageView, null);
    }

    data.VkSwapchain.DestroySwapchain(data.Device, data.Swapchain, null);
  }

  private Extent2D ChooseSwapExtent(SurfaceCapabilitiesKHR capabilities) {
    if (capabilities.CurrentExtent.Width != uint.MaxValue) {
      return capabilities.CurrentExtent;
    }

    var actualExtent = new Extent2D { Height = (uint)larkWindow.rawWindow.FramebufferSize.Y, Width = (uint)larkWindow.rawWindow.FramebufferSize.X };
    actualExtent.Width = new[]
    {
                capabilities.MinImageExtent.Width,
                new[] {capabilities.MaxImageExtent.Width, actualExtent.Width}.Min()
            }.Max();
    actualExtent.Height = new[]
    {
                capabilities.MinImageExtent.Height,
                new[] {capabilities.MaxImageExtent.Height, actualExtent.Height}.Min()
            }.Max();

    return actualExtent;
  }

  private PresentModeKHR ChooseSwapPresentMode(PresentModeKHR[] presentModes) {
    foreach (var availablePresentMode in presentModes) {
      if (availablePresentMode == PresentModeKHR.MailboxKhr) {
        return availablePresentMode;
      }
    }

    return PresentModeKHR.FifoKhr;
  }

  private SurfaceFormatKHR ChooseSwapSurfaceFormat(SurfaceFormatKHR[] formats) {
    foreach (var format in formats) {
      if (format.Format == Format.B8G8R8A8Unorm) {
        return format;
      }
    }

    return formats[0];
  }
}