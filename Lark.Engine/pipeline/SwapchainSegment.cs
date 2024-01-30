using Lark.Engine.Model;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Image = Silk.NET.Vulkan.Image;

namespace Lark.Engine.pipeline;

public class SwapchainSegment(LarkVulkanData data,
    LarkWindow larkWindow,
    SwapchainSupportUtil swapchainSupportUtil,
    QueueFamilyUtil queueFamilyUtil,
    ImageViewSegment imageViewSegment,
    RenderPassSegment renderPassSegment,
    GraphicsPipelineSegment graphicsPipelineSegment,
    DepthSegment depthSegment,
    FramebufferSegment framebufferSegment,
    UniformBufferSegment uniformBufferSegment,
    DescriptorSetSegment descriptorSetSegment,
    ModelUtils modelUtils,
    CommandBufferSegment commandBufferSegment,
    ILogger<SwapchainSegment> logger
    ) {
  public unsafe void RecreateSwapChain() {
    Vector2D<int> framebufferSize = larkWindow.FramebufferSize;

    while (framebufferSize.X == 0 || framebufferSize.Y == 0) {
      framebufferSize = larkWindow.FramebufferSize;
      larkWindow.DoEvents();
    }

    _ = data.vk.DeviceWaitIdle(data.Device);

    CleanupSwapchain();

    // TODO: On SDL it is possible to get an invalid swap chain when the window is minimized.
    // This check can be removed when the above frameBufferSize check catches it.
    while (!CreateSwapChain()) {
      larkWindow.DoEvents();
    }

    CreateSwapChain();
    imageViewSegment.CreateImageViews();
    renderPassSegment.CreateRenderPass();
    graphicsPipelineSegment.CreateGraphicsPipeline();
    depthSegment.CreateDepthResources();
    framebufferSegment.CreateFramebuffers();
    uniformBufferSegment.CreateUniformBuffer();
    // descriptorSetSegment.CreateDescriptorPool();
    // descriptorSetSegment.CreateDescriptorSets();

    foreach (var (_, model) in data.models) {
      modelUtils.CreateDescriptorPool(model);
      modelUtils.CreateDescriptorSets(model);
    }

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

    if (indices.GraphicsFamily == null || indices.PresentFamily == null) {
      throw new Exception("Failed to find queue families.");
    }

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

      if (data.vk.CurrentDevice is null)
        throw new Exception("Current device is null.");

      if (!data.vk.TryGetDeviceExtension(data.Instance, data.vk.CurrentDevice.Value, out data.VkSwapchain)) {
        throw new NotSupportedException("KHR_data.Swapchain extension not found.");
      }

      if (data.VkSwapchain is null)
        throw new Exception("VkSwapchain is null.");

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

    logger.LogInformation("Created swapchain.");

    return true;
  }

  public unsafe void CleanupSwapchain() {
    data.vk.DestroyImageView(data.Device, data.DepthImageView, null);
    data.vk.DestroyImage(data.Device, data.DepthImage, null);
    data.vk.FreeMemory(data.Device, data.DepthImageMemory, null);

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

    if (data.VkSwapchain is null)
      throw new Exception("VkSwapchain is null.");

    data.VkSwapchain.DestroySwapchain(data.Device, data.Swapchain, null);
  }

  private Extent2D ChooseSwapExtent(SurfaceCapabilitiesKHR capabilities) {
    if (capabilities.CurrentExtent.Width != uint.MaxValue) {
      return capabilities.CurrentExtent;
    }

    var actualExtent = new Extent2D { Height = (uint)larkWindow.FramebufferSize.Y, Width = (uint)larkWindow.FramebufferSize.X };
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