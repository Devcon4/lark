using Lark.Engine.Model;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Lark.Engine.pipeline;

public class CommandBufferSegment(LarkVulkanData data, ModelUtils modelUtils, CommandUtils commandUtils, ILogger<CommandBufferSegment> logger) {

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

    var renderPassInfo = new RenderPassBeginInfo {
      SType = StructureType.RenderPassBeginInfo,
      RenderPass = data.RenderPass,
      Framebuffer = data.SwapchainFramebuffers[index],
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

    data.vk.CmdBeginRenderPass(data.CommandBuffers[index], &renderPassInfo, SubpassContents.Inline);

    // SetViewport
    // var viewport = new Viewport {
    //   X = 0.0f,
    //   Y = data.SwapchainExtent.Height, // flip y axis 
    //   Width = data.SwapchainExtent.Width,
    //   Height = -data.SwapchainExtent.Height, // flip y axis
    //   MinDepth = 0.0f,
    //   MaxDepth = 1.0f
    // };
    var viewport = new Viewport {
      X = 0.0f,
      Y = 0f, // flip y axis 
      Width = data.SwapchainExtent.Width,
      Height = data.SwapchainExtent.Height,
      MinDepth = 0.0f,
      MaxDepth = 1.0f
    };

    data.vk.CmdSetViewport(data.CommandBuffers[index], 0, 1, &viewport);
    var scissor = new Rect2D { Offset = default, Extent = data.SwapchainExtent };
    data.vk.CmdSetScissor(data.CommandBuffers[index], 0, 1, &scissor);

    data.vk.CmdBindPipeline(data.CommandBuffers[index], PipelineBindPoint.Graphics, data.GraphicsPipeline);

    foreach (var (key, instance) in data.instances) {
      var model = data.models[instance.ModelId];
      modelUtils.Draw(instance.Transform, model, index);
    }
    data.vk.CmdEndRenderPass(data.CommandBuffers[index]);

    if (data.vk.EndCommandBuffer(data.CommandBuffers[index]) != Result.Success) {
      throw new Exception("failed to record command buffer!");
    }
  }

  public unsafe void ResetCommandBuffer(CommandBuffer commandBuffer) {
    data.vk.ResetCommandBuffer(commandBuffer, CommandBufferResetFlags.None);
  }
}