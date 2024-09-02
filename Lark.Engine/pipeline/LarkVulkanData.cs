using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Semaphore = Silk.NET.Vulkan.Semaphore;
using Buffer = Silk.NET.Vulkan.Buffer;
using Image = Silk.NET.Vulkan.Image;
using Silk.NET.Maths;
using Lark.Engine.model;
using System.Diagnostics;
using System.Collections.Concurrent;
using Lark.Engine.gi;

namespace Lark.Engine.pipeline;

public class LarkVulkanData {
  public Stopwatch sw = new();

  public readonly Vk vk = Vk.GetApi();
  public const int MaxFramesInFlight = 3;

  public Dictionary<Guid, LarkInstance> instances = new();
  public Dictionary<Guid, LarkModel> models = new();
  public Dictionary<Guid, LarkCamera> cameras = new();
  public Dictionary<Guid, LarkLight> lights = new();

  public bool EnableValidationLayers = true;

  public Instance Instance;
  public DebugUtilsMessengerEXT DebugMessenger;
  public SurfaceKHR Surface;
  public PhysicalDevice PhysicalDevice;
  public Device Device;

  public Queue GraphicsQueue;
  public Queue PresentQueue;

  public SwapchainKHR Swapchain;
  public Memory<LarkImage> SwapchainImages = new(new LarkImage[MaxFramesInFlight]);
  // public Image[] SwapchainImages = null!;
  public Format SwapchainImageFormat;
  public Extent2D SwapchainExtent;
  // public ImageView[] SwapchainImageViews = null!;
  public CommandPool CommandPool;
  public DescriptorPool DescriptorPool;
  public DescriptorSet[] DescriptorSets = null!;

  public Image[] SwapChainImages = null!;

  public LarkBuffer[] UniformBuffers = null!;
  public CommandBuffer[] CommandBuffers = null!;

  public Semaphore[] ImageAvailableSemaphores = null!;
  public Semaphore[] RenderFinishedSemaphores = null!;
  public Fence[] InFlightFences = null!;
  public Fence[] ImagesInFlight = null!;

  public Fence CommandFence;

  public uint CurrentFrame;

  public bool FramebufferResized;

  public KhrSurface? VkSurface;
  public KhrSwapchain? VkSwapchain;
  public ExtDebugUtils? DebugUtils;

  public string[]? ValidationLayers;

  public string[] InstanceExtensions = { ExtDebugUtils.ExtensionName };
  public string[] DeviceExtensions = { KhrSwapchain.ExtensionName };
  public int CurrF;
  public readonly string[][] ValidationLayerNamesPriorityList = new string[][] {
    new [] { "VK_LAYER_KHRONOS_validation" }, new [] { "VK_LAYER_LUNARG_standard_validation" }, new [] {
    "VK_LAYER_GOOGLE_threading",
    "VK_LAYER_LUNARG_parameter_validation",
    "VK_LAYER_LUNARG_object_tracker",
    "VK_LAYER_LUNARG_core_validation",
    "VK_LAYER_GOOGLE_unique_objects",
    }
  };
}