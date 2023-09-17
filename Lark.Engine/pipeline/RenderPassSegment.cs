using Silk.NET.Vulkan;

namespace Lark.Engine.Pipeline;

public class RenderPassSegment(LarkVulkanData data) {
  public unsafe void CreateRenderPass() {
    var colorAttachment = new AttachmentDescription {
      Format = data.SwapchainImageFormat,
      Samples = SampleCountFlags.Count1Bit,
      LoadOp = AttachmentLoadOp.Clear,
      StoreOp = AttachmentStoreOp.Store,
      StencilLoadOp = AttachmentLoadOp.DontCare,
      StencilStoreOp = AttachmentStoreOp.DontCare,
      InitialLayout = ImageLayout.Undefined,
      FinalLayout = ImageLayout.PresentSrcKhr
    };

    var colorAttachmentRef = new AttachmentReference {
      Attachment = 0,
      Layout = ImageLayout.ColorAttachmentOptimal
    };

    var subpass = new SubpassDescription {
      PipelineBindPoint = PipelineBindPoint.Graphics,
      ColorAttachmentCount = 1,
      PColorAttachments = &colorAttachmentRef
    };

    var dependency = new SubpassDependency {
      SrcSubpass = Vk.SubpassExternal,
      DstSubpass = 0,
      SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
      SrcAccessMask = 0,
      DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
      DstAccessMask = AccessFlags.ColorAttachmentReadBit | AccessFlags.ColorAttachmentWriteBit
    };

    var renderPassInfo = new RenderPassCreateInfo {
      SType = StructureType.RenderPassCreateInfo,
      AttachmentCount = 1,
      PAttachments = &colorAttachment,
      SubpassCount = 1,
      PSubpasses = &subpass,
      DependencyCount = 1,
      PDependencies = &dependency
    };

    fixed (RenderPass* renderPass = &data.RenderPass) {
      if (data.vk.CreateRenderPass(data.Device, &renderPassInfo, null, renderPass) != Result.Success) {
        throw new Exception("failed to create render pass!");
      }
    }
  }
}