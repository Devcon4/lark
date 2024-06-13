using Lark.Engine.Model;
using Lark.Engine.Ultralight;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Lark.Engine.pipeline;

public class CommandBufferSegment(LarkVulkanData data, IEnumerable<ILarkPipeline> pipelines) {

  public unsafe void CreateCommandBuffers() {
    data.CommandBuffers = new CommandBuffer[LarkVulkanData.MaxFramesInFlight];
    var allocInfo = new CommandBufferAllocateInfo {
      SType = StructureType.CommandBufferAllocateInfo,
      CommandPool = data.CommandPool,
      Level = CommandBufferLevel.Primary,
      CommandBufferCount = (uint)data.CommandBuffers.Length
    };

    fixed (CommandBuffer* commandBuffersPtr = data.CommandBuffers) {
      if (data.vk.AllocateCommandBuffers(data.Device, allocInfo, commandBuffersPtr) != Result.Success) {
        throw new Exception("failed to allocate command buffers!");
      }
    }
  }

  public unsafe void RecordCommandBuffer(CommandBuffer commandBuffer, uint index) {
    var beginInfo = new CommandBufferBeginInfo {
      SType = StructureType.CommandBufferBeginInfo,
    };

    if (data.vk.BeginCommandBuffer(commandBuffer, &beginInfo) != Result.Success) {
      throw new Exception("failed to begin recording command buffer!");
    }
    var viewport = new Viewport {
      X = 0.0f,
      Y = 0f, // flip y axis 
      Width = data.SwapchainExtent.Width,
      Height = data.SwapchainExtent.Height,
      MinDepth = 0.0f,
      MaxDepth = 1.0f
    };

    var scissor = new Rect2D { Offset = default, Extent = data.SwapchainExtent };

    foreach (var pipeline in pipelines) {

      var renderPassBeginInfo = new RenderPassBeginInfo {
        SType = StructureType.RenderPassBeginInfo,
        RenderPass = pipeline.Data.RenderPass,
        Framebuffer = pipeline.Data.Framebuffers[index],
        RenderArea = { Offset = new Offset2D { X = 0, Y = 0 }, Extent = data.SwapchainExtent }
      };

      fixed (ClearValue* clearValuesPtr = pipeline.Data.clearValues) {
        renderPassBeginInfo.ClearValueCount = (uint)pipeline.Data.clearValues.Length;
        renderPassBeginInfo.PClearValues = clearValuesPtr;
      }

      data.vk.CmdBeginRenderPass(data.CommandBuffers[index], &renderPassBeginInfo, SubpassContents.Inline);
      data.vk.CmdSetViewport(data.CommandBuffers[index], 0, 1, &viewport);
      data.vk.CmdSetScissor(data.CommandBuffers[index], 0, 1, &scissor);

      data.vk.CmdBindPipeline(data.CommandBuffers[index], PipelineBindPoint.Graphics, pipeline.Data.Pipeline);

      pipeline.Draw(index);

      data.vk.CmdEndRenderPass(data.CommandBuffers[index]);

    }

    if (data.vk.EndCommandBuffer(data.CommandBuffers[index]) != Result.Success) {
      throw new Exception("failed to record command buffer!");
    }
  }

  public unsafe void ResetCommandBuffer(CommandBuffer commandBuffer) {
    data.vk.ResetCommandBuffer(commandBuffer, CommandBufferResetFlags.None);
  }
}