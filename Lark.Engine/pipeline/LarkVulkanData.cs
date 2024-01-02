using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Semaphore = Silk.NET.Vulkan.Semaphore;
using Buffer = Silk.NET.Vulkan.Buffer;
using Image = Silk.NET.Vulkan.Image;
using Silk.NET.Maths;
using Lark.Engine.Model;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace Lark.Engine.Pipeline;

public struct DescriptorLayouts {
  public DescriptorSetLayout matricies;
  public DescriptorSetLayout textures;
}

public class LarkVulkanData {
  public Stopwatch sw = new();

  public readonly Vk vk = Vk.GetApi();
  public const int MaxFramesInFlight = 3;

  public Dictionary<Guid, LarkInstance> instances = new();
  public Dictionary<Guid, LarkModel> models = new();
  public Dictionary<Guid, LarkCamera> cameras = new();

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
  public DescriptorLayouts Layouts;

  public PipelineLayout PipelineLayout;
  public Silk.NET.Vulkan.Pipeline GraphicsPipeline;

  // public Buffer VertexBuffer;
  // public DeviceMemory VertexBufferMemory;
  // public Buffer IndexBuffer;
  // public DeviceMemory IndexBufferMemory;

  public Image NormalImage;
  public DeviceMemory NormalImageMemory;
  public ImageView NormalImageView;
  public Sampler NormalSampler;

  public Image TextureImage;
  public DeviceMemory TextureImageMemory;
  public ImageView TextureImageView;
  public Sampler TextureSampler;

  public Image DepthImage;
  public DeviceMemory DepthImageMemory;
  public ImageView DepthImageView;


  public CommandPool CommandPool;
  public DescriptorPool DescriptorPool;
  public DescriptorSet[] DescriptorSets = null!;

  public Image[] SwapChainImages = null!;

  public LarkBuffer[] UniformBuffers;

  // public Buffer[] UniformBuffers = null!;
  // public DeviceMemory[] UniformBuffersMemory = null!;
  // public unsafe void*[] UniformBuffersMapped = null!;

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

  public string[] InstanceExtensions = { }; // Laptop can't use VK_KHR_debug_utils for some reason. Just disable it for now.
  // public string[] InstanceExtensions = { ExtDebugUtils.ExtensionName };
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

  // public Vertex[] Vertices = new Vertex[]
  //   {

  //     new Vertex { Pos = new Vector3D<float>(-0.5f,-0.5f, 0f), Color = new Vector3D<float>(1.0f, 0.0f, 0.0f), UV = new Vector2D<float>(1.0f, 0.0f) },
  //     new Vertex { Pos = new Vector3D<float>(0.5f,-0.5f, 0f), Color = new Vector3D<float>(0.0f, 1.0f, 0.0f), UV = new Vector2D<float>(0.0f, 0.0f) },
  //     new Vertex { Pos = new Vector3D<float>(0.5f,0.5f, 0f), Color = new Vector3D<float>(0.0f, 0.0f, 1.0f), UV = new Vector2D<float>(0.0f, 1.0f) },
  //     new Vertex { Pos = new Vector3D<float>(-0.5f,0.5f, 0f), Color = new Vector3D<float>(1.0f, 1.0f, 1.0f), UV = new Vector2D<float>(1.0f, 1.0f) },

  //     new Vertex { Pos = new Vector3D<float>(-0.5f,-0.5f, -0.5f), Color = new Vector3D<float>(1.0f, 0.0f, 0.0f), UV = new Vector2D<float>(1.0f, 0.0f) },
  //     new Vertex { Pos = new Vector3D<float>(0.5f,-0.5f, -0.5f), Color = new Vector3D<float>(0.0f, 1.0f, 0.0f), UV = new Vector2D<float>(0.0f, 0.0f) },
  //     new Vertex { Pos = new Vector3D<float>(0.5f,0.5f, -0.5f), Color = new Vector3D<float>(0.0f, 0.0f, 1.0f), UV = new Vector2D<float>(0.0f, 1.0f) },
  //     new Vertex { Pos = new Vector3D<float>(-0.5f,0.5f, -0.5f), Color = new Vector3D<float>(1.0f, 1.0f, 1.0f), UV = new Vector2D<float>(1.0f, 1.0f) },
  //   };

  // public ushort[] Indices = new ushort[]
  // {
  //       0, 1, 2, 2, 3, 0,
  //       4, 5, 6, 6, 7, 4
  // };

  // public Memory<Vertex> Vertices = new(new Vertex[]{
  //   new(new(-0.5f, -0.5f, 0.0f), new(0.0f, 0.0f), new(1.0f, 0.0f, 0.0f)),

  //   new(new(0.5f, -0.5f, 0.0f), new(1.0f, 0.0f), new(0.0f, 1.0f, 0.0f)),
  //   new(new(0.5f, 0.5f, 0.0f), new(1.0f, 1.0f), new(0.0f, 0.0f, 1.0f)),
  //   new(new(-0.5f, 0.5f, 0.0f), new(0.0f, 1.0f), new(1.0f, 1.0f, 1.0f)),
  //   new(new(-0.5f, -0.5f, -0.5f), new(0.0f, 0.0f), new(1.0f, 0.0f, 0.0f)),
  //   new(new(0.5f, -0.5f, -0.5f), new(1.0f, 0.0f), new(0.0f, 1.0f, 0.0f)),
  //   new(new(0.5f, 0.5f, -0.5f), new(1.0f, 1.0f), new(0.0f, 0.0f, 1.0f)),
  //   new(new(-0.5f, 0.5f, -0.5f), new(0.0f, 1.0f), new(1.0f, 1.0f, 1.0f))
  // });

  // public Memory<ushort> Indices = new(new ushort[] { 0, 1, 2, 2, 3, 0, 4, 5, 6, 6, 7, 4 });
}