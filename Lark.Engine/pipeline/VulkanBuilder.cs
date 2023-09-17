using Microsoft.Extensions.Logging;
using Silk.NET.Vulkan;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Lark.Engine.Pipeline;

public class VulkanBuilder(
    ILogger<VulkanBuilder> logger,
    LarkVulkanData larkVulkanData,
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
  public const bool EventBasedRendering = false;

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
      larkVulkanData.vk.DestroySemaphore(larkVulkanData.Device, larkVulkanData.RenderFinishedSemaphores[i], null);
      larkVulkanData.vk.DestroySemaphore(larkVulkanData.Device, larkVulkanData.ImageAvailableSemaphores[i], null);
      larkVulkanData.vk.DestroyFence(larkVulkanData.Device, larkVulkanData.InFlightFences[i], null);
    }

    larkVulkanData.vk.DestroyCommandPool(larkVulkanData.Device, larkVulkanData.CommandPool, null);

    larkVulkanData.vk.DestroyDevice(larkVulkanData.Device, null);

    if (larkVulkanData.EnableValidationLayers) {
      larkVulkanData.DebugUtils?.DestroyDebugUtilsMessenger(larkVulkanData.Instance, larkVulkanData.DebugMessenger, null);
    }

    larkVulkanData.VkSurface?.DestroySurface(larkVulkanData.Instance, larkVulkanData.Surface, null);
    larkVulkanData.vk.DestroyInstance(larkVulkanData.Instance, null);
  }

  public void Wait() {
    larkVulkanData.vk.DeviceWaitIdle(larkVulkanData.Device);
  }

  public unsafe void DrawFrame(double obj) {
    var fence = larkVulkanData.InFlightFences[larkVulkanData.CurrentFrame];
    larkVulkanData.vk.WaitForFences(larkVulkanData.Device, 1, in fence, Vk.True, ulong.MaxValue);

    if (larkVulkanData.VkSwapchain is null) throw new Exception("Swapchain is null");

    uint imageIndex;
    Result result = larkVulkanData.VkSwapchain.AcquireNextImage
        (larkVulkanData.Device, larkVulkanData.Swapchain, ulong.MaxValue, larkVulkanData.ImageAvailableSemaphores[larkVulkanData.CurrentFrame], default, &imageIndex);

    if (result == Result.ErrorOutOfDateKhr) {
      swapchainSegment.RecreateSwapChain();
      return;
    }
    else if (result != Result.Success && result != Result.SuboptimalKhr) {
      throw new Exception("failed to acquire swap chain image!");
    }

    if (larkVulkanData.ImagesInFlight[imageIndex].Handle != 0) {
      larkVulkanData.vk.WaitForFences(larkVulkanData.Device, 1, in larkVulkanData.ImagesInFlight[imageIndex], Vk.True, ulong.MaxValue);
    }

    larkVulkanData.ImagesInFlight[imageIndex] = larkVulkanData.InFlightFences[larkVulkanData.CurrentFrame];

    SubmitInfo submitInfo = new SubmitInfo { SType = StructureType.SubmitInfo };

    Semaphore[] waitSemaphores = { larkVulkanData.ImageAvailableSemaphores[larkVulkanData.CurrentFrame] };
    PipelineStageFlags[] waitStages = { PipelineStageFlags.ColorAttachmentOutputBit };
    submitInfo.WaitSemaphoreCount = 1;
    var signalSemaphore = larkVulkanData.RenderFinishedSemaphores[larkVulkanData.CurrentFrame];
    fixed (Semaphore* waitSemaphoresPtr = waitSemaphores) {
      fixed (PipelineStageFlags* waitStagesPtr = waitStages) {
        submitInfo.PWaitSemaphores = waitSemaphoresPtr;
        submitInfo.PWaitDstStageMask = waitStagesPtr;

        submitInfo.CommandBufferCount = 1;
        var buffer = larkVulkanData.CommandBuffers[imageIndex];
        submitInfo.PCommandBuffers = &buffer;

        submitInfo.SignalSemaphoreCount = 1;
        submitInfo.PSignalSemaphores = &signalSemaphore;

        larkVulkanData.vk.ResetFences(larkVulkanData.Device, 1, &fence);

        if (larkVulkanData.vk.QueueSubmit
                (larkVulkanData.GraphicsQueue, 1, &submitInfo, larkVulkanData.InFlightFences[larkVulkanData.CurrentFrame]) != Result.Success) {
          throw new Exception("failed to submit draw command buffer!");
        }
      }
    }

    fixed (SwapchainKHR* swapchain = &larkVulkanData.Swapchain) {
      PresentInfoKHR presentInfo = new PresentInfoKHR {
        SType = StructureType.PresentInfoKhr,
        WaitSemaphoreCount = 1,
        PWaitSemaphores = &signalSemaphore,
        SwapchainCount = 1,
        PSwapchains = swapchain,
        PImageIndices = &imageIndex
      };

      result = larkVulkanData.VkSwapchain.QueuePresent(larkVulkanData.PresentQueue, &presentInfo);
    }

    if (result == Result.ErrorOutOfDateKhr || result == Result.SuboptimalKhr || larkVulkanData.FramebufferResized) {
      larkVulkanData.FramebufferResized = false;
      swapchainSegment.RecreateSwapChain();
    }
    else if (result != Result.Success) {
      throw new Exception("failed to present swap chain image!");
    }

    larkVulkanData.CurrentFrame = (larkVulkanData.CurrentFrame + 1) % LarkVulkanData.MaxFramesInFlight;
  }
}
