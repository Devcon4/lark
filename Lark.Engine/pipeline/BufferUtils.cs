using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
namespace Lark.Engine.pipeline;

public struct BufferAllocInfo {
  public BufferUsageFlags Usage;
  public MemoryPropertyFlags Properties;
  public SharingMode SharingMode;
}

public struct LarkBuffer {
  public Buffer Buffer;
  public DeviceMemory Memory;

  public unsafe void Dispose(LarkVulkanData data) {
    data.vk.DestroyBuffer(data.Device, Buffer, null);
    data.vk.FreeMemory(data.Device, Memory, null);
  }
}

public class BufferUtils(LarkVulkanData data, CommandUtils commandUtils) {

  // CreateBuffer: wrapper which uses a LarkBuffer struct.
  public unsafe void CreateBuffer(ulong size, BufferAllocInfo allocInfo, ref LarkBuffer buffer) {
    CreateBuffer(size, allocInfo, ref buffer.Buffer, ref buffer.Memory);
  }

  public unsafe void CreateBuffer(ulong size, BufferAllocInfo allocInfo, ref Buffer buffer, ref DeviceMemory bufferMemory) {
    var bufferInfo = new BufferCreateInfo {
      SType = StructureType.BufferCreateInfo,
      Size = size,
      Usage = allocInfo.Usage,
      SharingMode = allocInfo.SharingMode
    };

    data.vk.CreateBuffer(data.Device, &bufferInfo, null, out buffer);

    var memRequirements = new MemoryRequirements();
    data.vk.GetBufferMemoryRequirements(data.Device, buffer, &memRequirements);

    var vkAllocInfo = new MemoryAllocateInfo {
      SType = StructureType.MemoryAllocateInfo,
      AllocationSize = memRequirements.Size,
      MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits, allocInfo.Properties)
    };

    data.vk.AllocateMemory(data.Device, &vkAllocInfo, null, out bufferMemory);
    data.vk.BindBufferMemory(data.Device, buffer, bufferMemory, 0);
  }

  public unsafe uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties) {
    var memProperties = new PhysicalDeviceMemoryProperties();
    data.vk.GetPhysicalDeviceMemoryProperties(data.PhysicalDevice, &memProperties);

    for (var i = 0; i < memProperties.MemoryTypeCount; i++) {
      if ((typeFilter & (1 << i)) != 0 && (memProperties.MemoryTypes[i].PropertyFlags & properties) == properties) {
        return (uint)i;
      }
    }

    throw new Exception("failed to find suitable memory type!");
  }

  // Copy the contents of one buffer to another.
  public unsafe void CopyBuffer(Buffer srcBuffer, Buffer dstBuffer, ulong size) {
    var commandBuffer = commandUtils.BeginSingleTimeCommands();

    var copyRegion = new BufferCopy {
      Size = size
    };
    data.vk.CmdCopyBuffer(commandBuffer, srcBuffer, dstBuffer, 1, &copyRegion);

    commandUtils.EndSingleTimeCommands(commandBuffer);
  }
}