using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Lark.Engine.Pipeline;

public class LarkVulkanData {
  public readonly Vk vk = Vk.GetApi();
  public const int MaxFramesInFlight = 8;

  public bool EnableValidationLayers = true;

  public Instance Instance;
  public DebugUtilsMessengerEXT DebugMessenger;
  public SurfaceKHR Surface;
  public PhysicalDevice PhysicalDevice;
  public Device Device;

  public Queue GraphicsQueue;
  public Queue PresentQueue;

  public SwapchainKHR Swapchain;
  public Image[] SwapchainImages = null!;
  public Format SwapchainImageFormat;
  public Extent2D SwapchainExtent;
  public ImageView[] SwapchainImageViews = null!;
  public Framebuffer[] SwapchainFramebuffers = null!;

  public RenderPass RenderPass;
  public PipelineLayout PipelineLayout;
  public Silk.NET.Vulkan.Pipeline GraphicsPipeline;

  public CommandPool CommandPool;
  public CommandBuffer[] CommandBuffers = null!;

  public Semaphore[] ImageAvailableSemaphores = null!;
  public Semaphore[] RenderFinishedSemaphores = null!;
  public Fence[] InFlightFences = null!;
  public Fence[] ImagesInFlight = null!;
  public uint CurrentFrame;

  public bool FramebufferResized;

  public KhrSurface? VkSurface;
  public KhrSwapchain? VkSwapchain;
  public ExtDebugUtils? DebugUtils;

  public string[]? ValidationLayers;

  public string[] InstanceExtensions = { ExtDebugUtils.ExtensionName };
  public string[] DeviceExtensions = { KhrSwapchain.ExtensionName };
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