using Silk.NET.Vulkan;

namespace Lark.Engine.Pipeline;

public class CommandPoolSegment(LarkVulkanData data, QueueFamilyUtil queueFamilyUtil) {
  public unsafe void CreateCommandPool() {
    var queueFamilyIndices = queueFamilyUtil.FindQueueFamilies(data.PhysicalDevice);

    if (queueFamilyIndices.GraphicsFamily == null) {
      throw new Exception("failed to find graphics queue family!");
    }

    var poolInfo = new CommandPoolCreateInfo {
      SType = StructureType.CommandPoolCreateInfo,
      QueueFamilyIndex = queueFamilyIndices.GraphicsFamily.Value,
    };

    fixed (CommandPool* commandPool = &data.CommandPool) {
      if (data.vk.CreateCommandPool(data.Device, &poolInfo, null, commandPool) != Result.Success) {
        throw new Exception("failed to create command pool!");
      }
    }
  }

}