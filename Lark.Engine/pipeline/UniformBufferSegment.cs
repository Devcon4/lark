using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lark.Engine.Model;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Lark.Engine.Pipeline;

public struct UniformBufferObject {
  public Matrix4X4<float> model;
  public Matrix4X4<float> view;
  public Matrix4X4<float> proj;

  public Vector3D<float> lightPos;
  public Vector3D<float> viewPos;


}

public class UniformBufferSegment(LarkVulkanData data, LarkWindow larkWindow, BufferUtils bufferUtils, ILogger<UniformBufferSegment> logger) {

  // public unsafe void CreateUniformBuffers() {
  //   var bufferSize = (uint)sizeof(UniformBufferObject);

  //   data.UniformBuffers = new Buffer[LarkVulkanData.MaxFramesInFlight];
  //   data.UniformBuffersMemory = new DeviceMemory[LarkVulkanData.MaxFramesInFlight];

  //   var allocInfo = new BufferAllocInfo {
  //     Usage = BufferUsageFlags.UniformBufferBit,
  //     Properties = MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
  //     SharingMode = SharingMode.Exclusive
  //   };

  //   for (var i = 0; i < LarkVulkanData.MaxFramesInFlight; i++) {
  //     bufferUtils.CreateBuffer(bufferSize, allocInfo, ref data.UniformBuffers[i], ref data.UniformBuffersMemory[i]);
  //   }

  //   logger.LogInformation("Created uniform buffers.");
  // }

  // Same as above but only create one buffer. Use a LarkBuffer.
  public unsafe void CreateUniformBuffer() {
    var bufferSize = (uint)sizeof(UniformBufferObject);

    var allocInfo = new BufferAllocInfo {
      Usage = BufferUsageFlags.UniformBufferBit,
      Properties = MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
      SharingMode = SharingMode.Exclusive
    };

    data.UniformBuffers = new LarkBuffer[LarkVulkanData.MaxFramesInFlight];
    // Create a LarkBuffer for every frame in flight.
    for (var i = 0; i < LarkVulkanData.MaxFramesInFlight; i++) {
      bufferUtils.CreateBuffer(bufferSize, allocInfo, ref data.UniformBuffers[i]);
    }

    logger.LogInformation("Created uniform buffers.");
  }

  public unsafe void UpdateUniformBuffer(LarkCamera camera, uint currentFrame) {
    var time = (float)larkWindow.Time;
    // Build ubo from camera.
    var uboData = new UniformBufferObject {
      view = Matrix4X4.Transform(Matrix4X4.CreateTranslation(camera.Transform.Translation), camera.Transform.Rotation),
      proj = Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(camera.Fov), camera.AspectRatio, camera.Near, camera.Far),
      lightPos = new Vector3D<float>(0, 10, 0),
      viewPos = camera.Transform.Translation
    };

    // var old = new UniformBufferObject {
    //   model = Matrix4X4.CreateFromAxisAngle<float>(new Vector3D<float>(0, 1, 0), time * Scalar.DegreesToRadians(20f)),
    //   view = Matrix4X4.CreateLookAt(new Vector3D<float>(0, -1, 3), new Vector3D<float>(0, 0, 0), new Vector3D<float>(0, -1, 0)),
    //   proj = Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(45.0f), (float)data.SwapchainExtent.Width / data.SwapchainExtent.Height, 0.1f, 100.0f),
    // };

    // var firstModel = data.models[0];
    // if (firstModel != null) {
    //   // Create view matrix that looks at the model.
    //   var model = firstModel.Transform;
    //   var camera = new Vector3D<float>(10, 18, 0);
    //   var view = Matrix4X4.CreateLookAt(camera, model.Translation, new Vector3D<float>(0, 0, 1));
    //   uboData.view = view;
    // }

    // convert uboData.model to a direction vector.
    var direction = new Vector3D<float>(uboData.model.M13, uboData.model.M23, uboData.model.M33);

    // Flip the Y coordinate because Vulkan is left handed.
    uboData.proj.M22 *= -1;

    void* dataPtr;
    data.vk.MapMemory(data.Device, data.UniformBuffers[currentFrame].Memory, 0, (ulong)sizeof(UniformBufferObject), 0, &dataPtr);
    new Span<UniformBufferObject>(dataPtr, 1)[0] = uboData;
    data.vk.UnmapMemory(data.Device, data.UniformBuffers[currentFrame].Memory);
  }

  public unsafe void CleanupUniformBuffers() {
    for (var i = 0; i < LarkVulkanData.MaxFramesInFlight; i++) {
      data.UniformBuffers[i].Dispose(data);
    }
  }

  // public unsafe void UpdateUniformBuffer(uint currentImage) {
  //   var time = (float)larkWindow.Time;

  //   var uboData = new UniformBufferObject {
  //     model = Matrix4X4<float>.Identity * Matrix4X4.CreateFromAxisAngle<float>(new Vector3D<float>(0, 0, 1), time * Scalar.DegreesToRadians(90.0f)),
  //     view = Matrix4X4.CreateLookAt(new Vector3D<float>(2, 2, 2), new Vector3D<float>(0, 0, 0), new Vector3D<float>(0, 0, 1)),
  //     proj = Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(45.0f), (float)data.SwapchainExtent.Width / data.SwapchainExtent.Height, 0.1f, 10.0f),
  //   };

  //   uboData.proj.M22 *= -1;

  //   void* dataPtr;
  //   data.vk.MapMemory(data.Device, data.UniformBuffersMemory[currentImage], 0, (ulong)sizeof(UniformBufferObject), 0, &dataPtr);
  //   new Span<UniformBufferObject>(dataPtr, 1)[0] = uboData;
  //   data.vk.UnmapMemory(data.Device, data.UniformBuffersMemory[currentImage]);
  // }

  // public unsafe void CleanupUniformBuffers() {
  //   for (var i = 0; i < LarkVulkanData.MaxFramesInFlight; i++) {
  //     data.vk.DestroyBuffer(data.Device, data.UniformBuffers[i], null);
  //     data.vk.FreeMemory(data.Device, data.UniformBuffersMemory[i], null);
  //   }
  // }
}