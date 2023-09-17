using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Lark.Engine.Pipeline;

public class LogicalDeviceSegment(LarkVulkanData data, QueueFamilyUtil queueFamilyUtil, ILogger<LogicalDeviceSegment> logger) {
  public unsafe void CreateLogicalDevice() {
    var indices = queueFamilyUtil.FindQueueFamilies(data.PhysicalDevice);
    var uniqueQueueFamilies = indices.GraphicsFamily.Value == indices.PresentFamily.Value
        ? new[] { indices.GraphicsFamily.Value }
        : new[] { indices.GraphicsFamily.Value, indices.PresentFamily.Value };

    using var mem = GlobalMemory.Allocate((int)uniqueQueueFamilies.Length * sizeof(DeviceQueueCreateInfo));
    var queueCreateInfos = (DeviceQueueCreateInfo*)Unsafe.AsPointer(ref mem.GetPinnableReference());

    var queuePriority = 1f;
    for (var i = 0; i < uniqueQueueFamilies.Length; i++) {
      var queueCreateInfo = new DeviceQueueCreateInfo {
        SType = StructureType.DeviceQueueCreateInfo,
        QueueFamilyIndex = uniqueQueueFamilies[i],
        QueueCount = 1,
        PQueuePriorities = &queuePriority
      };
      queueCreateInfos[i] = queueCreateInfo;
    }

    var deviceFeatures = new PhysicalDeviceFeatures();

    var createInfo = new DeviceCreateInfo();
    createInfo.SType = StructureType.DeviceCreateInfo;
    createInfo.QueueCreateInfoCount = (uint)uniqueQueueFamilies.Length;
    createInfo.PQueueCreateInfos = queueCreateInfos;
    createInfo.PEnabledFeatures = &deviceFeatures;
    createInfo.EnabledExtensionCount = (uint)data.DeviceExtensions.Length;

    var enabledExtensionNames = SilkMarshal.StringArrayToPtr(data.DeviceExtensions);
    createInfo.PpEnabledExtensionNames = (byte**)enabledExtensionNames;

    if (data.EnableValidationLayers) {
      createInfo.EnabledLayerCount = (uint)data.ValidationLayers.Length;
      createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(data.ValidationLayers);
    }
    else {
      createInfo.EnabledLayerCount = 0;
    }

    fixed (Device* device = &data.Device) {
      if (data.vk.CreateDevice(data.PhysicalDevice, &createInfo, null, device) != Result.Success) {
        throw new Exception("Failed to create logical device.");
      }
    }

    fixed (Queue* graphicsQueue = &data.GraphicsQueue) {
      data.vk.GetDeviceQueue(data.Device, indices.GraphicsFamily.Value, 0, graphicsQueue);
    }

    fixed (Queue* presentQueue = &data.PresentQueue) {
      data.vk.GetDeviceQueue(data.Device, indices.PresentFamily.Value, 0, presentQueue);
    }

    data.vk.CurrentDevice = data.Device;

    if (!data.vk.TryGetDeviceExtension(data.Instance, data.Device, out data.VkSwapchain)) {
      throw new NotSupportedException("KHR_data.Swapchain extension not found.");
    }

    logger.LogDebug($"{data.vk.CurrentInstance?.Handle} {data.vk.CurrentDevice?.Handle}");
  }
}