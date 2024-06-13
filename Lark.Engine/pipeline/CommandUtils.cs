using Microsoft.Extensions.Logging;
using Silk.NET.Vulkan;

namespace Lark.Engine.pipeline;

public class CommandUtils(LarkVulkanData data) {

  public unsafe CommandBuffer BeginSingleTimeCommands() {
    var allocInfo = new CommandBufferAllocateInfo {
      SType = StructureType.CommandBufferAllocateInfo,
      CommandPool = data.CommandPool,
      Level = CommandBufferLevel.Primary,
      CommandBufferCount = 1
    };

    data.vk.AllocateCommandBuffers(data.Device, allocInfo, out CommandBuffer commandBuffer);

    var beginInfo = new CommandBufferBeginInfo {
      SType = StructureType.CommandBufferBeginInfo,
      Flags = CommandBufferUsageFlags.OneTimeSubmitBit
    };

    data.vk.BeginCommandBuffer(commandBuffer, beginInfo);

    return commandBuffer;
  }

  public unsafe void EndSingleTimeCommands(CommandBuffer commandBuffer) {
    data.vk.EndCommandBuffer(commandBuffer);

    var submitInfo = new SubmitInfo {
      SType = StructureType.SubmitInfo,
      CommandBufferCount = 1,
      PCommandBuffers = &commandBuffer
    };

    data.vk.QueueSubmit(data.GraphicsQueue, 1, submitInfo, default);
    data.vk.QueueWaitIdle(data.GraphicsQueue);

    data.vk.FreeCommandBuffers(data.Device, data.CommandPool, 1, commandBuffer);
  }
}