using Silk.NET.Vulkan;

namespace Lark.Engine.Pipeline;

public class RenderPassSegment(LarkVulkanData data, ImageUtils imageUtils) {
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

    var depthAttachment = new AttachmentDescription {
      Format = imageUtils.FindDepthFormat(),
      Samples = SampleCountFlags.Count1Bit,
      LoadOp = AttachmentLoadOp.Clear,
      StoreOp = AttachmentStoreOp.DontCare,
      StencilLoadOp = AttachmentLoadOp.DontCare,
      StencilStoreOp = AttachmentStoreOp.DontCare,
      InitialLayout = ImageLayout.Undefined,
      FinalLayout = ImageLayout.DepthStencilAttachmentOptimal
    };

    var colorAttachmentRef = new AttachmentReference {
      Attachment = 0,
      Layout = ImageLayout.ColorAttachmentOptimal
    };

    var depthAttachmentRef = new AttachmentReference {
      Attachment = 1,
      Layout = ImageLayout.DepthStencilAttachmentOptimal
    };

    var subpass = new SubpassDescription {
      PipelineBindPoint = PipelineBindPoint.Graphics,
      ColorAttachmentCount = 1,
      PColorAttachments = &colorAttachmentRef,
      PDepthStencilAttachment = &depthAttachmentRef
    };

    var attachments = new[] { colorAttachment, depthAttachment };

    var dependency = new SubpassDependency() {
      SrcSubpass = Vk.SubpassExternal,
      DstSubpass = 0,
      SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
      SrcAccessMask = 0,
      DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
      DstAccessMask = AccessFlags.ColorAttachmentWriteBit | AccessFlags.DepthStencilAttachmentWriteBit
    };

    fixed (AttachmentDescription* attachmentsPtr = attachments) {
      var renderPassInfo = new RenderPassCreateInfo {
        SType = StructureType.RenderPassCreateInfo,
        AttachmentCount = (uint)attachments.Length,
        PAttachments = attachmentsPtr,
        SubpassCount = 1,
        PSubpasses = &subpass,
        DependencyCount = 1,
        PDependencies = &dependency
      };


      if (data.vk.CreateRenderPass(data.Device, &renderPassInfo, null, out data.RenderPass) != Result.Success) {
        throw new Exception("failed to create render pass!");
      }
    }
  }


}