
using System.Runtime.CompilerServices;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Lark.Engine.Pipeline;

public class QueueFamilyUtil(LarkVulkanData data) {

  // Caching these values might have unintended side effects
  public unsafe QueueFamilyIndices FindQueueFamilies(PhysicalDevice device) {
    var indices = new QueueFamilyIndices();

    uint queryFamilyCount = 0;
    data.vk.GetPhysicalDeviceQueueFamilyProperties(device, &queryFamilyCount, null);

    using var mem = GlobalMemory.Allocate((int)queryFamilyCount * sizeof(QueueFamilyProperties));
    var queueFamilies = (QueueFamilyProperties*)Unsafe.AsPointer(ref mem.GetPinnableReference());

    data.vk.GetPhysicalDeviceQueueFamilyProperties(device, &queryFamilyCount, queueFamilies);
    for (var i = 0u; i < queryFamilyCount; i++) {
      var queueFamily = queueFamilies[i];
      // note: HasFlag is slow on .NET Core 2.1 and below.
      // if you're targeting these versions, use ((queueFamily.QueueFlags & QueueFlags.QueueGraphicsBit) != 0)
      if (queueFamily.QueueFlags.HasFlag(QueueFlags.GraphicsBit)) {
        indices.GraphicsFamily = i;
      }

      if (data.VkSurface is null) continue;

      data.VkSurface.GetPhysicalDeviceSurfaceSupport(device, i, data.Surface, out var presentSupport);

      if (presentSupport == Vk.True) {
        indices.PresentFamily = i;
      }

      if (indices.IsComplete()) {
        break;
      }
    }

    return indices;
  }
}

public struct QueueFamilyIndices {
  public uint? GraphicsFamily { get; set; }
  public uint? PresentFamily { get; set; }

  public bool IsComplete() {
    return GraphicsFamily.HasValue && PresentFamily.HasValue;
  }
}