using Lark.Engine.Model;
using Lark.Engine.std;
using Lark.Engine.Ultralight;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Lark.Engine.pipeline;

public class VulkanBuilder(
    ILogger<VulkanBuilder> logger,
    LarkVulkanData data,
    InstanceSegment instanceSegment,
    DebugSegment debugSegment,
    SurfaceSegment surfaceSegment,
    PhysicalDeviceSegment physicalDeviceSegment,
    LogicalDeviceSegment logicalDeviceSegment,
    SwapchainSegment swapchainSegment,
    UniformBufferSegment uniformBufferSegment,
    ImageViewSegment imageViewSegment,
    CommandPoolSegment commandPoolSegment,
    CommandBufferSegment commandBufferSegment,
    SyncSegment syncSegment,
    TimeManager timeManager,
    IEnumerable<ILarkPipeline> pipelines
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
    commandPoolSegment.CreateCommandPool();
    uniformBufferSegment.CreateUniformBuffer();
    syncSegment.CreateSyncObjects();

    foreach (var pipeline in pipelines.OrderByDescending(p => p.Priority)) {
      pipeline.Start();
    }

    commandBufferSegment.CreateCommandBuffers();
  }

  public unsafe void Cleanup() {
    logger.LogInformation("Disposing Vulkan... {thread}", Environment.CurrentManagedThreadId);

    // wait for each in-flight frame to finish
    data.vk.DeviceWaitIdle(data.Device);

    foreach (var (_, model) in data.models) {
      model.Dispose(data);
    }

    foreach (var pipeline in pipelines.OrderByDescending(p => p.Priority)) {
      pipeline.Cleanup();
    }

    uniformBufferSegment.CleanupUniformBuffers();
    swapchainSegment.CleanupSwapchain();

    for (var i = 0; i < LarkVulkanData.MaxFramesInFlight; i++) {
      data.vk.DestroySemaphore(data.Device, data.RenderFinishedSemaphores[i], null);
      data.vk.DestroySemaphore(data.Device, data.ImageAvailableSemaphores[i], null);
      data.vk.DestroyFence(data.Device, data.InFlightFences[i], null);
    }

    data.vk.DestroyFence(data.Device, data.CommandFence, null);
    data.vk.DestroyCommandPool(data.Device, data.CommandPool, null);

    data.vk.DestroyDevice(data.Device, null);

    if (data.EnableValidationLayers) {
      data.DebugUtils?.DestroyDebugUtilsMessenger(data.Instance, data.DebugMessenger, null);
    }

    data.VkSurface?.DestroySurface(data.Instance, data.Surface, null);
    data.vk.DestroyInstance(data.Instance, null);

    data.vk.Dispose();
  }

  public void Wait() {
    data.vk.DeviceWaitIdle(data.Device);
  }

  // currentFrame

  public unsafe void DrawFrame() {

    var d = data;

    var renderingCamera = d.cameras.Values.FirstOrDefault(c => c.Active, LarkCamera.DefaultCamera());

    logger.LogInformation("{frame} :: RenderCamera position :: {position}", timeManager.TotalFrames, renderingCamera.Transform.Translation);

    // logger.LogInformation("{currentF} \t:: Δ {deltaTime}ms \t:: {fps}", currentF, deltaTime.TotalMilliseconds, fps);

    data.vk.WaitForFences(data.Device, 1, data.InFlightFences[data.CurrentFrame], true, ulong.MaxValue);
    uint imageIndex = 0;
    var result = data.VkSwapchain!.AcquireNextImage(data.Device, data.Swapchain, ulong.MaxValue, data.ImageAvailableSemaphores[data.CurrentFrame], default, &imageIndex);

    if (result == Result.ErrorOutOfDateKhr) {
      swapchainSegment.RecreateSwapChain();
      return;
    }
    else if (result != Result.Success && result != Result.SuboptimalKhr) {
      throw new Exception("failed to acquire swap chain image!");
    }

    if (data.ImagesInFlight[imageIndex].Handle != default) {
      data.vk.WaitForFences(data.Device, 1, data.ImagesInFlight[imageIndex], true, ulong.MaxValue);
    }

    foreach (var pipeline in pipelines.OrderByDescending(p => p.Priority)) {
      pipeline.Update(imageIndex);
    }

    uniformBufferSegment.UpdateUniformBuffer(renderingCamera, data.CurrentFrame);

    commandBufferSegment.ResetCommandBuffer(data.CommandBuffers[imageIndex]);
    commandBufferSegment.RecordCommandBuffer(data.CommandBuffers[imageIndex], imageIndex);

    data.ImagesInFlight[imageIndex] = data.InFlightFences[data.CurrentFrame];

    SubmitInfo submitInfo = new SubmitInfo { SType = StructureType.SubmitInfo };

    var waitSemaphores = stackalloc[] { data.ImageAvailableSemaphores[data.CurrentFrame] };
    var waitStages = stackalloc[] { PipelineStageFlags.ColorAttachmentOutputBit };

    var buffer = data.CommandBuffers[imageIndex];

    submitInfo = submitInfo with {
      WaitSemaphoreCount = 1,
      PWaitSemaphores = waitSemaphores,
      PWaitDstStageMask = waitStages,

      CommandBufferCount = 1,
      PCommandBuffers = &buffer,
    };

    var signalSemaphore = stackalloc[] { data.RenderFinishedSemaphores[data.CurrentFrame] };
    submitInfo = submitInfo with {
      SignalSemaphoreCount = 1,
      PSignalSemaphores = signalSemaphore,
    };

    data.vk.ResetFences(data.Device, 1, data.InFlightFences[data.CurrentFrame]);

    if (data.vk.QueueSubmit(data.GraphicsQueue, 1, &submitInfo, data.InFlightFences[data.CurrentFrame]) != Result.Success) {
      throw new Exception("failed to submit draw command buffer!");
    }

    var swapchains = stackalloc[] { data.Swapchain };

    var presentInfo = new PresentInfoKHR {
      SType = StructureType.PresentInfoKhr,
      WaitSemaphoreCount = 1,
      PWaitSemaphores = signalSemaphore,
      SwapchainCount = 1,
      PSwapchains = swapchains,
      PImageIndices = &imageIndex
    };

    result = data.VkSwapchain.QueuePresent(data.PresentQueue, &presentInfo);

    if (result == Result.ErrorOutOfDateKhr || result == Result.SuboptimalKhr || data.FramebufferResized) {
      data.FramebufferResized = false;
      swapchainSegment.RecreateSwapChain();
    }
    else if (result != Result.Success) {
      throw new Exception("failed to present swap chain image!");
    }

    data.CurrentFrame = (data.CurrentFrame + 1) % LarkVulkanData.MaxFramesInFlight;
    timeManager.Update();

  }

  public void FramebufferResize(Vector2D<int> size) {
    data.FramebufferResized = true;
    // swapchainSegment.RecreateSwapChain();
    // larkWindow.rawWindow.DoRender();
  }
}
