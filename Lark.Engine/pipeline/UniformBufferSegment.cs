using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lark.Engine.model;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Lark.Engine.pipeline;

public struct UniformBufferObject {
  public Matrix4X4<float> model;
  public Matrix4X4<float> view;
  public Matrix4X4<float> proj;

  public Vector3D<float> lightPos;
  public Vector3D<float> viewPos;


}

public class UniformBufferSegment(LarkVulkanData data, LarkWindow larkWindow, BufferUtils bufferUtils, ILogger<UniformBufferSegment> logger) {

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
      view = camera.View.ToGeneric(),
      proj = camera.Projection.ToGeneric(),
      lightPos = new Vector3D<float>(0, -10, 0),
      viewPos = camera.Transform.Translation
    };

    // Because we flip the viewport we shouldn't need to flip the Y coordinate anymore.
    // Flip the Y coordinate because Vulkan is left handed.
    // uboData.proj.M22 *= -1;

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
}