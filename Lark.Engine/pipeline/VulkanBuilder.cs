using Lark.Engine.Model;
using Lark.Engine.std;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Lark.Engine.pipeline;

public class VulkanBuilder(
    ILogger<VulkanBuilder> logger,
    LarkVulkanData data,
    InstanceSegment instanceSegment,
    DebugSegment debugSegment,
    DescriptorSetSegment descriptorSetSegment,
    SurfaceSegment surfaceSegment,
    PhysicalDeviceSegment physicalDeviceSegment,
    LogicalDeviceSegment logicalDeviceSegment,
    SwapchainSegment swapchainSegment,
    UniformBufferSegment uniformBufferSegment,
    ImageViewSegment imageViewSegment,
    RenderPassSegment renderPassSegment,
    GraphicsPipelineSegment graphicsPipelineSegment,
    FramebufferSegment framebufferSegment,
    TextureSegment textureSegment,
    SamplerSegment samplerSegment,
    CommandPoolSegment commandPoolSegment,
    CommandBufferSegment commandBufferSegment,
    SyncSegment syncSegment,
    MeshBufferSegment meshBufferSegment,
    DepthSegment depthSegment,
    ModelUtils modelUtils,
    TimeManager timeManager
    ) {

  public void InitVulkan() {
    logger.LogInformation("Initializing Vulkan...");

    instanceSegment.CreateInstance();
    debugSegment.SetupDebugMessenger();
    surfaceSegment.CreateSurface();
    physicalDeviceSegment.PickPhysicalDevice();
    logicalDeviceSegment.CreateLogicalDevice();
    swapchainSegment.CreateSwapChain();
    imageViewSegment.CreateImageViews();
    renderPassSegment.CreateRenderPass();
    descriptorSetSegment.CreateDescriptorSetLayouts();
    // descriptorSetSegment.CreateDescriptorSetLayout();
    graphicsPipelineSegment.CreateGraphicsPipeline();
    commandPoolSegment.CreateCommandPool();
    depthSegment.CreateDepthResources();

    framebufferSegment.CreateFramebuffers();
    textureSegment.CreateTextureImage();
    textureSegment.CreateTextureImageView();
    samplerSegment.CreateTextureSampler();

    uniformBufferSegment.CreateUniformBuffer();
    // descriptorSetSegment.CreateDescriptorPool();
    // descriptorSetSegment.CreateDescriptorSets();
    syncSegment.CreateSyncObjects();

    // data.cameras.Add(LarkCamera.DefaultCamera());

    // data.models.Add(modelUtils.LoadFile("damagedHelmet/DamagedHelmet.glb"));
    // data.models.Add(modelUtils.LoadFile("damagedHelmet/DamagedHelmet.gltf"));
    // data.models.Add(modelUtils.LoadFile("fish/BarramundiFish.gltf"));
    // data.models.Add(modelUtils.LoadFile("boxTextured/BoxTextured.glb"));
    // data.models.Add(modelUtils.LoadFile("materialsVariantsShoe/MaterialsVariantsShoe.glb"));

    // data.models.Add(modelUtils.LoadFile("orientationTest/OrientationTest.glb"));
    // data.models.Add(modelUtils.LoadFile("antiqueCamera/AntiqueCamera.gltf"));
    // data.models.Add(modelUtils.LoadFile("metalRoughSpheres/MetalRoughSpheres.glb"));
    // data.models.Add(modelUtils.LoadFile("stainedGlassLamp/gLTF/StainedGlassLamp.gltf"));

    commandBufferSegment.CreateCommandBuffers();
  }

  public unsafe void Cleanup() {
    logger.LogInformation("Disposing Vulkan... {thread}", Environment.CurrentManagedThreadId);

    // wait for each in-flight frame to finish
    data.vk.DeviceWaitIdle(data.Device);

    foreach (var (_, model) in data.models) {
      model.Dispose(data);
    }

    uniformBufferSegment.CleanupUniformBuffers();
    swapchainSegment.CleanupSwapchain();

    data.vk.DestroySampler(data.Device, data.TextureSampler, null);
    data.vk.DestroyImageView(data.Device, data.TextureImageView, null);

    data.vk.DestroyImage(data.Device, data.TextureImage, null);
    data.vk.FreeMemory(data.Device, data.TextureImageMemory, null);

    data.vk.DestroyDescriptorSetLayout(data.Device, data.Layouts.matricies, null);
    data.vk.DestroyDescriptorSetLayout(data.Device, data.Layouts.textures, null);

    // Destroy descriptor set layout
    // data.vk.DestroyDescriptorSetLayout(data.Device, data.DescriptorSetLayout, null);

    // data.vk.DestroyBuffer(data.Device, data.IndexBuffer, null);
    // data.vk.FreeMemory(data.Device, data.IndexBufferMemory, null);

    // data.vk.DestroyBuffer(data.Device, data.VertexBuffer, null);
    // data.vk.FreeMemory(data.Device, data.VertexBufferMemory, null);

    for (var i = 0; i < LarkVulkanData.MaxFramesInFlight; i++) {
      data.vk.DestroySemaphore(data.Device, data.RenderFinishedSemaphores[i], null);
      data.vk.DestroySemaphore(data.Device, data.ImageAvailableSemaphores[i], null);
      data.vk.DestroyFence(data.Device, data.InFlightFences[i], null);
    }

    data.vk.DestroyFence(data.Device, data.CommandFence, null);
    data.vk.DestroyCommandPool(data.Device, data.CommandPool, null);

    data.vk.DestroyDevice(data.Device, null);

    if (data.EnableValidationLayers) {
      data.DebugUtils?.DestroyDebugUtilsMessenger(data.Instance, data.DebugMessenger, null);
    }

    data.VkSurface?.DestroySurface(data.Instance, data.Surface, null);
    data.vk.DestroyInstance(data.Instance, null);

    data.vk.Dispose();
  }

  public void Wait() {
    data.vk.DeviceWaitIdle(data.Device);
  }

  // currentFrame

  public unsafe void DrawFrame() {
    timeManager.Update();

    var d = data;

    var renderingCamera = d.cameras.Values.FirstOrDefault(c => c.Active, LarkCamera.DefaultCamera());

    // camera.SetPosition(new Vector3D<float>(5, 0, 0));
    // camera.SetRotation(new Vector3D<float>(0, -1, 0), 270);
    // camera.SetFov(45f);
    // // camera.Transform.RotateByAxisAndAngle(new Vector3D<float>(0, 1, 0), (float)deltaTime.TotalSeconds * .9f);
    // camera.SetAspectRatio((float)data.SwapchainExtent.Width / data.SwapchainExtent.Height);
    // vector from quaternion

    // var theta = Math.Acos(camera.Transform.Rotation.W) * 2;
    // var ax = camera.Transform.Rotation.X / Math.Sin(theta / 2);
    // var ay = camera.Transform.Rotation.Y / Math.Sin(theta / 2);
    // var az = camera.Transform.Rotation.Z / Math.Sin(theta / 2);
    // // split angle and axis
    // var angle = theta * 180 / Math.PI;
    // var axis = new Vector3D<float>((float)ax, (float)ay, (float)az);

    // firstModel.Transform.Translation = new Vector3D<float>(0, 0, 0);
    // firstModel.Transform.Scale = new Vector3D<float>(1, 1, 1);

    // data.cameras[0] = camera;
    // log ubo.

    // logger.LogInformation("{currentF} \t:: Δ {deltaTime}ms \t:: {fps}", currentF, deltaTime.TotalMilliseconds, fps);

    data.vk.WaitForFences(data.Device, 1, data.InFlightFences[data.CurrentFrame], true, ulong.MaxValue);
    uint imageIndex = 0;
    var result = data.VkSwapchain!.AcquireNextImage(data.Device, data.Swapchain, ulong.MaxValue, data.ImageAvailableSemaphores[data.CurrentFrame], default, &imageIndex);

    if (result == Result.ErrorOutOfDateKhr) {
      swapchainSegment.RecreateSwapChain();
      return;
    }
    else if (result != Result.Success && result != Result.SuboptimalKhr) {
      throw new Exception("failed to acquire swap chain image!");
    }

    if (data.ImagesInFlight[imageIndex].Handle != default) {
      data.vk.WaitForFences(data.Device, 1, data.ImagesInFlight[imageIndex], true, ulong.MaxValue);
    }

    uniformBufferSegment.UpdateUniformBuffer(renderingCamera, data.CurrentFrame);

    commandBufferSegment.ResetCommandBuffer(data.CommandBuffers[imageIndex]);
    commandBufferSegment.RecordCommandBuffer(data.CommandBuffers[imageIndex], imageIndex);

    data.ImagesInFlight[imageIndex] = data.InFlightFences[data.CurrentFrame];

    SubmitInfo submitInfo = new SubmitInfo { SType = StructureType.SubmitInfo };

    var waitSemaphores = stackalloc[] { data.ImageAvailableSemaphores[data.CurrentFrame] };
    var waitStages = stackalloc[] { PipelineStageFlags.ColorAttachmentOutputBit };

    var buffer = data.CommandBuffers[imageIndex];

    submitInfo = submitInfo with {
      WaitSemaphoreCount = 1,
      PWaitSemaphores = waitSemaphores,
      PWaitDstStageMask = waitStages,

      CommandBufferCount = 1,
      PCommandBuffers = &buffer,
    };

    var signalSemaphore = stackalloc[] { data.RenderFinishedSemaphores[data.CurrentFrame] };
    submitInfo = submitInfo with {
      SignalSemaphoreCount = 1,
      PSignalSemaphores = signalSemaphore,
    };

    data.vk.ResetFences(data.Device, 1, data.InFlightFences[data.CurrentFrame]);

    if (data.vk.QueueSubmit(data.GraphicsQueue, 1, &submitInfo, data.InFlightFences[data.CurrentFrame]) != Result.Success) {
      throw new Exception("failed to submit draw command buffer!");
    }

    var swapchains = stackalloc[] { data.Swapchain };

    var presentInfo = new PresentInfoKHR {
      SType = StructureType.PresentInfoKhr,
      WaitSemaphoreCount = 1,
      PWaitSemaphores = signalSemaphore,
      SwapchainCount = 1,
      PSwapchains = swapchains,
      PImageIndices = &imageIndex
    };

    result = data.VkSwapchain.QueuePresent(data.PresentQueue, &presentInfo);

    if (result == Result.ErrorOutOfDateKhr || result == Result.SuboptimalKhr || data.FramebufferResized) {
      data.FramebufferResized = false;
      swapchainSegment.RecreateSwapChain();
    }
    else if (result != Result.Success) {
      throw new Exception("failed to present swap chain image!");
    }

    data.CurrentFrame = (data.CurrentFrame + 1) % LarkVulkanData.MaxFramesInFlight;

  }

  public void FramebufferResize(Vector2D<int> size) {
    data.FramebufferResized = true;
    // swapchainSegment.RecreateSwapChain();
    // larkWindow.rawWindow.DoRender();
  }
}
