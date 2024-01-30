using Microsoft.Extensions.Logging;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using System.Runtime.InteropServices;

namespace Lark.Engine.pipeline;

public class InstanceSegment(LarkWindow window, LarkVulkanData data) {
  private unsafe string[]? GetOptimalValidationLayers() {
    var layerCount = 0u;
    data.vk.EnumerateInstanceLayerProperties(&layerCount, (LayerProperties*)0);

    var availableLayers = new LayerProperties[layerCount];
    fixed (LayerProperties* availableLayersPtr = availableLayers) {
      data.vk.EnumerateInstanceLayerProperties(&layerCount, availableLayersPtr);
    }

    var availableLayerNames = availableLayers.Select(availableLayer => Marshal.PtrToStringAnsi((nint)availableLayer.LayerName)).ToArray();
    foreach (var validationLayerNameSet in data.ValidationLayerNamesPriorityList) {
      if (validationLayerNameSet.All(validationLayerName => availableLayerNames.Contains(validationLayerName))) {
        return validationLayerNameSet;
      }
    }

    return null;
  }

  public unsafe void CreateInstance() {
    if (window.VulkanSupported() is false) {
      throw new NotSupportedException("Windowing platform doesn't support Vulkan.");
    }

    if (data.EnableValidationLayers) {
      data.ValidationLayers = GetOptimalValidationLayers();
      if (data.ValidationLayers is null) {
        throw new NotSupportedException("Validation layers requested, but not available!");
      }
    }

    var appInfo = new ApplicationInfo {
      SType = StructureType.ApplicationInfo,
      PApplicationName = (byte*)Marshal.StringToHGlobalAnsi("Lark Game"),
      ApplicationVersion = new Version32(1, 0, 0),
      PEngineName = (byte*)Marshal.StringToHGlobalAnsi("Lark Engine"),
      EngineVersion = new Version32(1, 0, 0),
      ApiVersion = Vk.Version10
    };

    var createInfo = new InstanceCreateInfo {
      SType = StructureType.InstanceCreateInfo,
      PApplicationInfo = &appInfo,
    };

    var extensions = window.GetRequiredInstanceExtensions(out var extCount);

    // Log extensions and extcount

    // var extensions = window.rawWindow.VkSurface.GetRequiredExtensions(out var extCount);
    // TODO Review that this count doesn't realistically exceed 1k (recommended max for stackalloc)
    // Should probably be allocated on heap anyway as this isn't super performance critical.
    var newExtensions = stackalloc byte*[(int)(extCount + data.InstanceExtensions.Length)];
    for (var i = 0; i < extCount; i++) {
      newExtensions[i] = extensions[i];
    }

    for (var i = 0; i < data.InstanceExtensions.Length; i++) {
      newExtensions[extCount + i] = (byte*)SilkMarshal.StringToPtr(data.InstanceExtensions[i]);
    }

    extCount += (uint)data.InstanceExtensions.Length;
    createInfo.EnabledExtensionCount = extCount;
    createInfo.PpEnabledExtensionNames = newExtensions;

    if (data.EnableValidationLayers) {

      if (data.ValidationLayers is null) {
        throw new Exception("Validation layers requested, but not available!");
      }
      createInfo.EnabledLayerCount = (uint)data.ValidationLayers.Length;
      createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(data.ValidationLayers);
    }
    else {
      createInfo.EnabledLayerCount = 0;
      createInfo.PNext = null;
    }

    fixed (Instance* instance = &data.Instance) {
      var res = data.vk.CreateInstance(&createInfo, null, instance);
      if (res != Result.Success) {
        throw new Exception("Failed to create instance!");
      }
    }

    data.vk.CurrentInstance = data.Instance;

    if (!data.vk.TryGetInstanceExtension(data.Instance, out data.VkSurface)) {
      throw new NotSupportedException("KHR_data.Surface extension not found.");
    }

    Marshal.FreeHGlobal((nint)appInfo.PApplicationName);
    Marshal.FreeHGlobal((nint)appInfo.PEngineName);

    if (data.EnableValidationLayers) {
      SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
    }
  }
}