using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Lark.Engine.Pipeline;

public class UniformBufferSegment(LarkVulkanData data, BufferUtils bufferUtils, ILogger<UniformBufferSegment> logger) {

  public unsafe void CreateUniformBuffers() {
    var bufferSize = (uint)sizeof(UniformBufferObject);

    data.UniformBuffers = new Buffer[LarkVulkanData.MaxFramesInFlight];
    data.UniformBuffersMemory = new DeviceMemory[LarkVulkanData.MaxFramesInFlight];

    var allocInfo = new BufferAllocInfo {
      Usage = BufferUsageFlags.UniformBufferBit,
      Properties = MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
      SharingMode = SharingMode.Exclusive
    };

    for (var i = 0; i < LarkVulkanData.MaxFramesInFlight; i++) {

      bufferUtils.CreateBuffer(bufferSize, allocInfo, out var uniformBuffer, out var uniformBufferMemory);

      data.UniformBuffers[i] = uniformBuffer;
      data.UniformBuffersMemory[i] = uniformBufferMemory;
    }

    logger.LogInformation("Created uniform buffers.");
  }

  public unsafe void CleanupUniformBuffers() {
    for (var i = 0; i < LarkVulkanData.MaxFramesInFlight; i++) {
      data.vk.DestroyBuffer(data.Device, data.UniformBuffers[i], null);
      data.vk.FreeMemory(data.Device, data.UniformBuffersMemory[i], null);
    }
  }
}