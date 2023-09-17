using System.Runtime.InteropServices;
using Silk.NET.Vulkan;

namespace Lark.Engine.Pipeline;
public class DebugSegment(LarkVulkanData data) {
  public unsafe void SetupDebugMessenger() {
    if (!data.EnableValidationLayers) return;
    if (!data.vk.TryGetInstanceExtension(data.Instance, out data.DebugUtils)) return;

    var createInfo = new DebugUtilsMessengerCreateInfoEXT();
    populateDebugMessengerCreateInfo(ref createInfo);

    fixed (DebugUtilsMessengerEXT* debugMessenger = &data.DebugMessenger) {
      if (data.DebugUtils.CreateDebugUtilsMessenger(data.Instance, &createInfo, null, debugMessenger) != Result.Success) {
        throw new Exception("Failed to create debug messenger.");
      }
    }
  }

  private unsafe void populateDebugMessengerCreateInfo(ref DebugUtilsMessengerCreateInfoEXT createInfo) {
    createInfo.SType = StructureType.DebugUtilsMessengerCreateInfoExt;
    createInfo.MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt |
      DebugUtilsMessageSeverityFlagsEXT.WarningBitExt |
      DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt;
    createInfo.MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt |
      DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt |
      DebugUtilsMessageTypeFlagsEXT.ValidationBitExt;
    createInfo.PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT)DebugCallback;
  }
  private unsafe uint DebugCallback(
      DebugUtilsMessageSeverityFlagsEXT messageSeverity,
      DebugUtilsMessageTypeFlagsEXT messageTypes,
      DebugUtilsMessengerCallbackDataEXT* pCallbackData,
      void* pUserData) {
    if (messageSeverity > DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt) {
      Console.WriteLine($"{messageSeverity} {messageTypes}" + Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage));
    }

    return Vk.False;
  }
}