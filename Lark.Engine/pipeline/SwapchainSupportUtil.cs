using System.Runtime.CompilerServices;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Lark.Engine.pipeline;

public class SwapchainSupportUtil(LarkVulkanData data) {

  // Caching the returned values breaks the ability for resizing the window
  public unsafe SwapChainSupportDetails QuerySwapChainSupport(PhysicalDevice device) {
    var details = new SwapChainSupportDetails();

    if (data.VkSurface is null) throw new Exception("Surface is null");

    data.VkSurface.GetPhysicalDeviceSurfaceCapabilities(device, data.Surface, out var surfaceCapabilities);
    details.Capabilities = surfaceCapabilities;

    var formatCount = 0u;
    data.VkSurface.GetPhysicalDeviceSurfaceFormats(device, data.Surface, &formatCount, null);

    if (formatCount != 0) {
      details.Formats = new SurfaceFormatKHR[formatCount];

      using var mem = GlobalMemory.Allocate((int)formatCount * sizeof(SurfaceFormatKHR));
      var formats = (SurfaceFormatKHR*)Unsafe.AsPointer(ref mem.GetPinnableReference());

      data.VkSurface.GetPhysicalDeviceSurfaceFormats(device, data.Surface, &formatCount, formats);

      for (var i = 0; i < formatCount; i++) {
        details.Formats[i] = formats[i];
      }
    }

    var presentModeCount = 0u;
    data.VkSurface.GetPhysicalDeviceSurfacePresentModes(device, data.Surface, &presentModeCount, null);

    if (presentModeCount != 0) {
      details.PresentModes = new PresentModeKHR[presentModeCount];

      using var mem = GlobalMemory.Allocate((int)presentModeCount * sizeof(PresentModeKHR));
      var modes = (PresentModeKHR*)Unsafe.AsPointer(ref mem.GetPinnableReference());

      data.VkSurface.GetPhysicalDeviceSurfacePresentModes(device, data.Surface, &presentModeCount, modes);

      for (var i = 0; i < presentModeCount; i++) {
        details.PresentModes[i] = modes[i];
      }
    }

    return details;
  }
}

public struct SwapChainSupportDetails {
  public SurfaceCapabilitiesKHR Capabilities { get; set; }
  public SurfaceFormatKHR[] Formats { get; set; }
  public PresentModeKHR[] PresentModes { get; set; }
}