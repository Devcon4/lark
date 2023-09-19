using Microsoft.Extensions.Logging;
using Silk.NET.Vulkan;

namespace Lark.Engine.Pipeline;

public class CommandUtils(LarkVulkanData data, ILogger<CommandUtils> logger) {

  public unsafe CommandBuffer BeginSingleTimeCommands() {
    var allocInfo = new CommandBufferAllocateInfo {
      SType = StructureType.CommandBufferAllocateInfo,
      CommandPool = data.CommandPool,
      Level = CommandBufferLevel.Primary,
      CommandBufferCount = 1
    };

    CommandBuffer commandBuffer;
    data.vk.AllocateCommandBuffers(data.Device, &allocInfo, &commandBuffer);

    var beginInfo = new CommandBufferBeginInfo {
      SType = StructureType.CommandBufferBeginInfo,
      Flags = CommandBufferUsageFlags.OneTimeSubmitBit
    };

    data.vk.BeginCommandBuffer(commandBuffer, &beginInfo);

    return commandBuffer;
  }

  public unsafe void EndSingleTimeCommands(CommandBuffer commandBuffer) {
    data.vk.EndCommandBuffer(commandBuffer);

    var submitInfo = new SubmitInfo {
      SType = StructureType.SubmitInfo,
      CommandBufferCount = 1,
      PCommandBuffers = &commandBuffer
    };

    data.vk.QueueSubmit(data.GraphicsQueue, 1, &submitInfo, data.CommandFence);
    data.vk.QueueWaitIdle(data.GraphicsQueue);

    data.vk.FreeCommandBuffers(data.Device, data.CommandPool, 1, &commandBuffer);
  }
}