using Silk.NET.Vulkan;

namespace Lark.Engine.Pipeline;

public class CommandBufferSegment(LarkVulkanData data) {
  public unsafe void CreateCommandBuffers() {
    data.CommandBuffers = new CommandBuffer[data.SwapchainFramebuffers.Length];

    var allocInfo = new CommandBufferAllocateInfo {
      SType = StructureType.CommandBufferAllocateInfo,
      CommandPool = data.CommandPool,
      Level = CommandBufferLevel.Primary,
      CommandBufferCount = (uint)data.CommandBuffers.Length
    };

    fixed (CommandBuffer* commandBuffers = data.CommandBuffers) {
      if (data.vk.AllocateCommandBuffers(data.Device, &allocInfo, commandBuffers) != Result.Success) {
        throw new Exception("failed to allocate command buffers!");
      }
    }

    for (var i = 0; i < data.CommandBuffers.Length; i++) {
      var beginInfo = new CommandBufferBeginInfo { SType = StructureType.CommandBufferBeginInfo };

      if (data.vk.BeginCommandBuffer(data.CommandBuffers[i], &beginInfo) != Result.Success) {
        throw new Exception("failed to begin recording command buffer!");
      }

      var renderPassInfo = new RenderPassBeginInfo {
        SType = StructureType.RenderPassBeginInfo,
        RenderPass = data.RenderPass,
        Framebuffer = data.SwapchainFramebuffers[i],
        RenderArea = { Offset = new Offset2D { X = 0, Y = 0 }, Extent = data.SwapchainExtent }
      };

      var clearColor = new ClearValue { Color = new ClearColorValue { Float32_0 = 0, Float32_1 = 0, Float32_2 = 0, Float32_3 = 1 } };
      renderPassInfo.ClearValueCount = 1;
      renderPassInfo.PClearValues = &clearColor;

      data.vk.CmdBeginRenderPass(data.CommandBuffers[i], &renderPassInfo, SubpassContents.Inline);

      data.vk.CmdBindPipeline(data.CommandBuffers[i], PipelineBindPoint.Graphics, data.GraphicsPipeline);

      data.vk.CmdDraw(data.CommandBuffers[i], 3, 1, 0, 0);

      data.vk.CmdEndRenderPass(data.CommandBuffers[i]);

      if (data.vk.EndCommandBuffer(data.CommandBuffers[i]) != Result.Success) {
        throw new Exception("failed to record command buffer!");
      }
    }
  }
}