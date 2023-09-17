using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Lark.Engine.Pipeline;

public class VulkanBuilder(
    ILogger<VulkanBuilder> logger,
    LarkVulkanData data,
    InstanceSegment instanceSegment,
    DebugSegment debugSegment,
    SurfaceSegment surfaceSegment,
    PhysicalDeviceSegment physicalDeviceSegment,
    LogicalDeviceSegment logicalDeviceSegment,
    SwapchainSegment swapchainSegment,
    ImageViewSegment imageViewSegment,
    RenderPassSegment renderPassSegment,
    GraphicsPipelineSegment graphicsPipelineSegment,
    FramebufferSegment framebufferSegment,
    CommandPoolSegment commandPoolSegment,
    CommandBufferSegment commandBufferSegment,
    SyncSegment syncSegment
    ) {
  public void InitVulkan() {
    logger.LogInformation("Initializing Vulkan...");

    instanceSegment.CreateInstance();
    debugSegment.SetupDebugMessenger();
    surfaceSegment.CreateSurface();
    physicalDeviceSegment.PickPhysicalDevice();
    logicalDeviceSegment.CreateLogicalDevice();
    swapchainSegment.CreateSwapChain();
    imageViewSegment.CreateImageViews();
    renderPassSegment.CreateRenderPass();
    graphicsPipelineSegment.CreateGraphicsPipeline();
    framebufferSegment.CreateFramebuffers();
    commandPoolSegment.CreateCommandPool();
    commandBufferSegment.CreateCommandBuffers();
    syncSegment.CreateSyncObjects();
  }

  public unsafe void Cleanup() {
    swapchainSegment.CleanupSwapchain();

    for (var i = 0; i < LarkVulkanData.MaxFramesInFlight; i++) {
      data.vk.DestroySemaphore(data.Device, data.RenderFinishedSemaphores[i], null);
      data.vk.DestroySemaphore(data.Device, data.ImageAvailableSemaphores[i], null);
      data.vk.DestroyFence(data.Device, data.InFlightFences[i], null);
    }

    data.vk.DestroyCommandPool(data.Device, data.CommandPool, null);

    data.vk.DestroyDevice(data.Device, null);

    if (data.EnableValidationLayers) {
      data.DebugUtils?.DestroyDebugUtilsMessenger(data.Instance, data.DebugMessenger, null);
    }

    data.VkSurface?.DestroySurface(data.Instance, data.Surface, null);
    data.vk.DestroyInstance(data.Instance, null);
  }

  public void Wait() {
    data.vk.DeviceWaitIdle(data.Device);
  }

  // currentFrame

  public int currentFrame = 0;
  public DateTime lastFrame = DateTime.Now;

  public unsafe void DrawFrame() {
    currentFrame++;

    // Time sense last frame
    var now = DateTime.Now;
    var deltaTime = now - lastFrame;
    lastFrame = now;

    var fps = 1 / deltaTime.TotalSeconds;

    // logger.LogInformation("{currentFrame} \t:: Δ {deltaTime}ms \t:: {fps}", currentFrame, deltaTime.TotalMilliseconds, fps);

    var fence = data.InFlightFences[data.CurrentFrame];
    data.vk.WaitForFences(data.Device, 1, in fence, Vk.True, ulong.MaxValue);

    if (data.VkSwapchain is null) throw new Exception("Swapchain is null");

    uint imageIndex;
    Result result = data.VkSwapchain.AcquireNextImage
        (data.Device, data.Swapchain, ulong.MaxValue, data.ImageAvailableSemaphores[data.CurrentFrame], default, &imageIndex);

    if (result == Result.ErrorOutOfDateKhr) {
      swapchainSegment.RecreateSwapChain();
      return;
    }
    else if (result != Result.Success && result != Result.SuboptimalKhr) {
      throw new Exception("failed to acquire swap chain image!");
    }

    if (data.ImagesInFlight[imageIndex].Handle != 0) {
      data.vk.WaitForFences(data.Device, 1, in data.ImagesInFlight[imageIndex], Vk.True, ulong.MaxValue);
    }

    data.ImagesInFlight[imageIndex] = data.InFlightFences[data.CurrentFrame];

    SubmitInfo submitInfo = new SubmitInfo { SType = StructureType.SubmitInfo };

    Semaphore[] waitSemaphores = { data.ImageAvailableSemaphores[data.CurrentFrame] };
    PipelineStageFlags[] waitStages = { PipelineStageFlags.ColorAttachmentOutputBit };
    submitInfo.WaitSemaphoreCount = 1;
    var signalSemaphore = data.RenderFinishedSemaphores[data.CurrentFrame];
    fixed (Semaphore* waitSemaphoresPtr = waitSemaphores) {
      fixed (PipelineStageFlags* waitStagesPtr = waitStages) {
        submitInfo.PWaitSemaphores = waitSemaphoresPtr;
        submitInfo.PWaitDstStageMask = waitStagesPtr;

        submitInfo.CommandBufferCount = 1;
        var buffer = data.CommandBuffers[imageIndex];
        submitInfo.PCommandBuffers = &buffer;

        submitInfo.SignalSemaphoreCount = 1;
        submitInfo.PSignalSemaphores = &signalSemaphore;

        data.vk.ResetFences(data.Device, 1, &fence);

        if (data.vk.QueueSubmit
                (data.GraphicsQueue, 1, &submitInfo, data.InFlightFences[data.CurrentFrame]) != Result.Success) {
          throw new Exception("failed to submit draw command buffer!");
        }
      }
    }

    fixed (SwapchainKHR* swapchain = &data.Swapchain) {
      PresentInfoKHR presentInfo = new PresentInfoKHR {
        SType = StructureType.PresentInfoKhr,
        WaitSemaphoreCount = 1,
        PWaitSemaphores = &signalSemaphore,
        SwapchainCount = 1,
        PSwapchains = swapchain,
        PImageIndices = &imageIndex
      };

      result = data.VkSwapchain.QueuePresent(data.PresentQueue, &presentInfo);
    }

    if (result == Result.ErrorOutOfDateKhr || result == Result.SuboptimalKhr || data.FramebufferResized) {
      data.FramebufferResized = false;
      swapchainSegment.RecreateSwapChain();
    }
    else if (result != Result.Success) {
      throw new Exception("failed to present swap chain image!");
    }

    data.CurrentFrame = (data.CurrentFrame + 1) % LarkVulkanData.MaxFramesInFlight;
  }

  public void FramebufferResize(Vector2D<int> size) {
    data.FramebufferResized = true;
    swapchainSegment.RecreateSwapChain();
    // larkWindow.rawWindow.DoRender();
  }
}
