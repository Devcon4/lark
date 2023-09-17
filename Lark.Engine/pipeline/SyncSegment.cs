using Silk.NET.Vulkan;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Lark.Engine.Pipeline;

public class SyncSegment(LarkVulkanData data) {
  public unsafe void CreateSyncObjects() {
    data.ImageAvailableSemaphores = new Semaphore[LarkVulkanData.MaxFramesInFlight];
    data.RenderFinishedSemaphores = new Semaphore[LarkVulkanData.MaxFramesInFlight];
    data.InFlightFences = new Fence[LarkVulkanData.MaxFramesInFlight];
    data.ImagesInFlight = new Fence[LarkVulkanData.MaxFramesInFlight];

    SemaphoreCreateInfo semaphoreInfo = new() {
      SType = StructureType.SemaphoreCreateInfo
    };

    FenceCreateInfo fenceInfo = new() {
      SType = StructureType.FenceCreateInfo,
      Flags = FenceCreateFlags.SignaledBit
    };

    for (var i = 0; i < LarkVulkanData.MaxFramesInFlight; i++) {
      Semaphore imgAvSema, renderFinSema;
      Fence inFlightFence;
      if (data.vk.CreateSemaphore(data.Device, &semaphoreInfo, null, &imgAvSema) != Result.Success ||
          data.vk.CreateSemaphore(data.Device, &semaphoreInfo, null, &renderFinSema) != Result.Success ||
          data.vk.CreateFence(data.Device, &fenceInfo, null, &inFlightFence) != Result.Success) {
        throw new Exception("failed to create synchronization objects for a frame!");
      }

      data.ImageAvailableSemaphores[i] = imgAvSema;
      data.RenderFinishedSemaphores[i] = renderFinSema;
      data.InFlightFences[i] = inFlightFence;
    }
  }
}