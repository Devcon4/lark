using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Silk.NET.Vulkan;

namespace Lark.Engine.pipeline;

public class PhysicalDeviceSegment(LarkVulkanData data, SwapchainSupportUtil swapchainSupportUtil, QueueFamilyUtil queueFamilyUtil, ILogger<PhysicalDeviceSegment> logger) {
  public unsafe void PickPhysicalDevice() {
    var devices = data.vk.GetPhysicalDevices(data.Instance);

    if (!devices.Any()) {
      throw new NotSupportedException("Failed to find GPUs with Vulkan support.");
    }

    data.PhysicalDevice = devices.FirstOrDefault(device => {
      var indices = queueFamilyUtil.FindQueueFamilies(device);

      var extensionsSupported = CheckDeviceExtensionSupport(device);

      var swapChainAdequate = false;
      if (extensionsSupported) {
        var swapChainSupport = swapchainSupportUtil.QuerySwapChainSupport(device);
        swapChainAdequate = swapChainSupport.Formats.Length != 0 && swapChainSupport.PresentModes.Length != 0;
      }

      return indices.IsComplete() && extensionsSupported && swapChainAdequate;
    });

    if (data.PhysicalDevice.Handle == 0)
      throw new Exception("No suitable device.");

  }

  private unsafe bool CheckDeviceExtensionSupport(PhysicalDevice device) {
    return data.DeviceExtensions.All(ext => data.vk.IsDeviceExtensionPresent(device, ext));
  }
}