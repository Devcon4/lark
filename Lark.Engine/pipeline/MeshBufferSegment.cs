using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Lark.Engine.Pipeline;

public struct Vertex(Vector3D<float> pos, Vector3D<float> normal, Vector2D<float> uv, Vector3D<float> color) {
  public Vector3D<float> Pos = pos;
  public Vector3D<float> Normal = normal;
  public Vector2D<float> UV = uv;
  public Vector3D<float> Color = color;

  public static VertexInputBindingDescription GetBindingDescription() {
    return new() {
      Binding = 0,
      Stride = (uint)Marshal.SizeOf<Vertex>(),
      InputRate = VertexInputRate.Vertex
    };
  }

  public static VertexInputAttributeDescription[] GetAttributeDescriptions() {
    return new[] {
      new VertexInputAttributeDescription {
        Binding = 0,
        Location = 0,
        Format = Format.R32G32B32Sfloat,
        Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(Pos))
      },
      new VertexInputAttributeDescription {
        Binding = 0,
        Location = 1,
        Format = Format.R32G32B32Sfloat,
        Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(Color))
      },
      new VertexInputAttributeDescription {
        Binding = 0,
        Location = 2,
        Format = Format.R32G32B32Sfloat,
        Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(UV))
      },
      new VertexInputAttributeDescription {
        Binding = 0,
        Location = 3,
        Format = Format.R32G32B32Sfloat,
        Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(Normal))
      }
    };
  }
}

public class MeshBufferSegment(LarkVulkanData data, BufferUtils bufferUtils, ILogger<MeshBufferSegment> logger) {

  // public unsafe void CreateVertexBuffer() {
  //   var bufferSize = (ulong)(Marshal.SizeOf<Vertex>() * data.Vertices.Length);

  //   Buffer stagingBuffer = default;
  //   DeviceMemory stagingBufferMemory = default;
  //   bufferUtils.CreateBuffer(
  //     bufferSize,
  //     new BufferAllocInfo {
  //       Usage = BufferUsageFlags.TransferSrcBit,
  //       Properties = MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
  //     },
  //     ref stagingBuffer,
  //     ref stagingBufferMemory
  //   );

  //   void* dataPtr;
  //   data.vk.MapMemory(data.Device, stagingBufferMemory, 0, bufferSize, 0, &dataPtr);
  //   data.Vertices.AsSpan().CopyTo(new Span<Vertex>(dataPtr, data.Vertices.Length));
  //   data.vk.UnmapMemory(data.Device, stagingBufferMemory);

  //   bufferUtils.CreateBuffer(
  //     bufferSize,
  //     new BufferAllocInfo {
  //       Usage = BufferUsageFlags.TransferDstBit | BufferUsageFlags.VertexBufferBit,
  //       Properties = MemoryPropertyFlags.DeviceLocalBit
  //     },
  //     ref data.VertexBuffer,
  //     ref data.VertexBufferMemory
  //   );

  //   bufferUtils.CopyBuffer(stagingBuffer, data.VertexBuffer, bufferSize);

  //   data.vk.DestroyBuffer(data.Device, stagingBuffer, null);
  //   data.vk.FreeMemory(data.Device, stagingBufferMemory, null);
  //   logger.LogInformation("Created vertex buffer");
  // }

  // CreateIndexBuffer.
  // public unsafe void CreateIndexBuffer() {
  //   var bufferSize = (ulong)(Unsafe.SizeOf<ushort>() * data.Indices.Length);

  //   Buffer stagingBuffer = default;
  //   DeviceMemory stagingBufferMemory = default;
  //   bufferUtils.CreateBuffer(
  //     bufferSize,
  //     new BufferAllocInfo {
  //       Usage = BufferUsageFlags.TransferSrcBit,
  //       Properties = MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit
  //     },
  //     ref stagingBuffer,
  //     ref stagingBufferMemory
  //   );

  //   void* dataPtr;
  //   data.vk.MapMemory(data.Device, stagingBufferMemory, 0, bufferSize, 0, &dataPtr);
  //   data.Indices.AsSpan().CopyTo(new Span<ushort>(dataPtr, data.Indices.Length));
  //   data.vk.UnmapMemory(data.Device, stagingBufferMemory);

  //   bufferUtils.CreateBuffer(
  //     bufferSize,
  //     new BufferAllocInfo {
  //       Usage = BufferUsageFlags.TransferDstBit | BufferUsageFlags.IndexBufferBit,
  //       Properties = MemoryPropertyFlags.DeviceLocalBit
  //     },
  //     ref data.IndexBuffer,
  //     ref data.IndexBufferMemory
  //   );

  //   bufferUtils.CopyBuffer(stagingBuffer, data.IndexBuffer, bufferSize);

  //   data.vk.DestroyBuffer(data.Device, stagingBuffer, null);
  //   data.vk.FreeMemory(data.Device, stagingBufferMemory, null);
  //   logger.LogInformation("Created index buffer");
  // }
}

