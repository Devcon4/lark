using Lark.Engine.Model;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Lark.Engine.Pipeline;

public class CommandBufferSegment(LarkVulkanData data, ModelUtils modelUtils, ILogger<CommandBufferSegment> logger) {
  public unsafe void CreateCommandBuffers() {
    data.CommandBuffers = new CommandBuffer[data.SwapchainFramebuffers.Length];

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

    for (var i = 0; i < data.CommandBuffers.Length; i++) {
      var beginInfo = new CommandBufferBeginInfo { SType = StructureType.CommandBufferBeginInfo };

      if (data.vk.BeginCommandBuffer(data.CommandBuffers[i], beginInfo) != Result.Success) {
        throw new Exception("failed to begin recording command buffer!");
      }

      var renderPassInfo = new RenderPassBeginInfo {
        SType = StructureType.RenderPassBeginInfo,
        RenderPass = data.RenderPass,
        Framebuffer = data.SwapchainFramebuffers[i],
        RenderArea = { Offset = new Offset2D { X = 0, Y = 0 }, Extent = data.SwapchainExtent }
      };

      var clearValues = new ClearValue[] {
        new() {
          Color = new ClearColorValue { Float32_0 = 0, Float32_1 = 0, Float32_2 = 0, Float32_3 = 1 }
        },
        new() {
          DepthStencil = new ClearDepthStencilValue { Depth = 1, Stencil = 0 }
        }
      };

      fixed (ClearValue* clearValuesPtr = clearValues) {
        renderPassInfo.ClearValueCount = (uint)clearValues.Length;
        renderPassInfo.PClearValues = clearValuesPtr;
      }

      data.vk.CmdBeginRenderPass(data.CommandBuffers[i], &renderPassInfo, SubpassContents.Inline);

      // SetViewport
      var viewport = new Viewport {
        X = 0.0f,
        Y = 0.0f,
        Width = data.SwapchainExtent.Width,
        Height = data.SwapchainExtent.Height,
        MinDepth = 0.0f,
        MaxDepth = 1.0f
      };

      data.vk.CmdSetViewport(data.CommandBuffers[i], 0, 1, &viewport);
      var scissor = new Rect2D { Offset = default, Extent = data.SwapchainExtent };
      data.vk.CmdSetScissor(data.CommandBuffers[i], 0, 1, &scissor);

      data.vk.CmdBindPipeline(data.CommandBuffers[i], PipelineBindPoint.Graphics, data.GraphicsPipeline);

      foreach (var model in data.models) {
        modelUtils.Draw(model, i);
      }

      data.vk.CmdEndRenderPass(data.CommandBuffers[i]);

      if (data.vk.EndCommandBuffer(data.CommandBuffers[i]) != Result.Success) {
        throw new Exception("failed to record command buffer!");
      }
    }

    logger.LogInformation("Created command buffers.");
  }

  // public unsafe void UpdateCommandBuffers(uint currentFrame) {
  //   for (var i = 0; i < data.CommandBuffers.Length; i++) {

  //     var beginInfo = new CommandBufferBeginInfo { SType = StructureType.CommandBufferBeginInfo };
  //     if (data.vk.BeginCommandBuffer(data.CommandBuffers[i], beginInfo) != Result.Success) {
  //       throw new Exception("failed to begin recording command buffer!");
  //     }

  //     foreach (var model in data.models) {
  //       modelUtils.Update(model, i);
  //     }

  //     if (data.vk.EndCommandBuffer(data.CommandBuffers[i]) != Result.Success) {
  //       throw new Exception("failed to record command buffer!");
  //     }
  //   }
  // }
}