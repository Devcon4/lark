using Lark.Engine.Ultralight;
using Microsoft.Extensions.Logging;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Lark.Engine.pipeline;

public class CompositePipeline(LarkVulkanData shareData, ILogger<CompositePipeline> logger, ImageUtils imageUtils, GeometryPassData geometryPass, LightingPassData lightingPass, UIPassData uiPass, ShaderBuilder shaderBuilder) : LarkPipeline(shareData) {

  // private Memory<DescriptorSet> GBufferSets = new DescriptorSet[LarkVulkanData.MaxFramesInFlight];
  // private Memory<DescriptorSet> LightSets = new DescriptorSet[LarkVulkanData.MaxFramesInFlight];
  // private Memory<DescriptorSet> UISets = new DescriptorSet[LarkVulkanData.MaxFramesInFlight];

  private Memory<DescriptorSet> SamplerSets = new DescriptorSet[LarkVulkanData.MaxFramesInFlight];

  public override int Priority => 512;

  public struct Layouts {
    public static string Sampelers = "Samplers";
    // public static string GBuffers = "GBuffers";
    // public static string LightBuffers = "LightBuffers";
    // public static string UIBuffers = "UIBuffers";
  }

  public override void Start() {
    CreateDescriptorPool();
    DeclarePipelineSets();
    CreateSetLayouts();
    CreateRenderPass();
    CreateFramebuffers();
    // CreateDescriptorSets();
    CreateSet(Layouts.Sampelers);
    CreateGraphicsPipeline();
    CreateClearValues();
  }

  public override void Draw(uint index) {
    TransitionGBufferImages((int)index, ImageLayout.ShaderReadOnlyOptimal);

    // UpdateDescriptorSets((int)index);
    // BindDescriptorSets((int)index);

    UpdateCompositeSets(index);
    BindSet(Layouts.Sampelers, index);

    shareData.vk.CmdDraw(shareData.CommandBuffers[index], 6, 1, 0, 0); // TODO: check this.
  }

  private unsafe void UpdateCompositeSets(uint index) {
    // geo
    var geoUpdateInfo = new DescriptorImageInfo {
      Sampler = geometryPass.Images.Span[(int)index].Sampler,
      ImageView = geometryPass.Images.Span[(int)index].View,
      ImageLayout = ImageLayout.ShaderReadOnlyOptimal
    };

    UpdateSet(Layouts.Sampelers, index, new WriteDescriptorSet {
      SType = StructureType.WriteDescriptorSet,
      DstBinding = 0,
      DstArrayElement = 0,
      DescriptorCount = 1,
      DescriptorType = DescriptorType.CombinedImageSampler,
      PImageInfo = &geoUpdateInfo
    });

    // light
    var lightUpdateInfo = new DescriptorImageInfo {
      Sampler = lightingPass.Images.Span[(int)index].Sampler,
      ImageView = lightingPass.Images.Span[(int)index].View,
      ImageLayout = ImageLayout.ShaderReadOnlyOptimal
    };

    UpdateSet(Layouts.Sampelers, index, new WriteDescriptorSet {
      SType = StructureType.WriteDescriptorSet,
      DstBinding = 1,
      DstArrayElement = 0,
      DescriptorCount = 1,
      DescriptorType = DescriptorType.CombinedImageSampler,
      PImageInfo = &lightUpdateInfo
    });

    // ui
    var uiUpdateInfo = new DescriptorImageInfo {
      Sampler = uiPass.FinalImages.Span[(int)index].Sampler,
      ImageView = uiPass.FinalImages.Span[(int)index].View,
      ImageLayout = ImageLayout.ShaderReadOnlyOptimal
    };

    UpdateSet(Layouts.Sampelers, index, new WriteDescriptorSet {
      SType = StructureType.WriteDescriptorSet,
      DstBinding = 2,
      DstArrayElement = 0,
      DescriptorCount = 1,
      DescriptorType = DescriptorType.CombinedImageSampler,
      PImageInfo = &uiUpdateInfo
    });
  }

  private void DeclarePipelineSets() {
    RegisterSet(Layouts.Sampelers, 0, [
      new LarkLayoutBindingInfo(DescriptorType.CombinedImageSampler, ShaderStageFlags.FragmentBit, 0),
      new LarkLayoutBindingInfo(DescriptorType.CombinedImageSampler, ShaderStageFlags.FragmentBit, 1),
      new LarkLayoutBindingInfo(DescriptorType.CombinedImageSampler, ShaderStageFlags.FragmentBit, 2),
    ]);
  }

  private void TransitionGBufferImages(int index, ImageLayout newLayout) {
    imageUtils.TransitionImageLayout(ref geometryPass.Images.Span[index], newLayout);
    imageUtils.TransitionImageLayout(ref geometryPass.Normals.Span[index], newLayout);
    imageUtils.TransitionImageLayout(ref lightingPass.Images.Span[index], newLayout);
    imageUtils.TransitionImageLayout(ref uiPass.FinalImages.Span[index], newLayout);
  }

  public unsafe override void Cleanup() {
    shareData.vk.DestroyDescriptorPool(shareData.Device, Data.DescriptorPool, null);

    foreach (var layout in Data.DescriptorSetLayouts.Values) {
      shareData.vk.DestroyDescriptorSetLayout(shareData.Device, layout, null);
    }

    foreach (var framebuffer in Data.Framebuffers) {
      shareData.vk.DestroyFramebuffer(shareData.Device, framebuffer, null);
    }

    shareData.vk.DestroyPipeline(shareData.Device, Data.Pipeline, null);
    shareData.vk.DestroyRenderPass(shareData.Device, Data.RenderPass, null);
    shareData.vk.DestroyPipelineLayout(shareData.Device, Data.PipelineLayout, null);
    base.Cleanup();
  }

  public unsafe void CreateFramebuffers() {
    Data.Framebuffers = new Framebuffer[LarkVulkanData.MaxFramesInFlight];

    for (var i = 0; i < LarkVulkanData.MaxFramesInFlight; i++) {
      var attachments = new ImageView[] {
        shareData.SwapchainImages.Span[i].View,
        geometryPass.Images.Span[i].View,
        lightingPass.Images.Span[i].View,
        uiPass.FinalImages.Span[i].View
      };

      var (attachmentsMem, attachmentsSize) = RegisterMemory(attachments);

      var framebufferCreateInfo = new FramebufferCreateInfo {
        SType = StructureType.FramebufferCreateInfo,
        RenderPass = Data.RenderPass,
        AttachmentCount = attachmentsSize,
        PAttachments = (ImageView*)attachmentsMem.Pointer,
        Width = shareData.SwapchainExtent.Width,
        Height = shareData.SwapchainExtent.Height,
        Layers = 1
      };

      if (shareData.vk.CreateFramebuffer(shareData.Device, &framebufferCreateInfo, null, out Data.Framebuffers[i]) != Result.Success) {
        throw new Exception("failed to create framebuffer!");
      }
    }
  }

  public void CreateClearValues() {
    Data.clearValues = [
      new() {
        Color = new ClearColorValue { Float32_0 = 0, Float32_1 = 0, Float32_2 = 0, Float32_3 = 0 }
      },
      new() {
        DepthStencil = new ClearDepthStencilValue { Depth = 1, Stencil = 0 }
      }
    ];
  }

  // public unsafe void CreateDescriptorSets() {
  //   // Create a descriptor set for each frame in flight
  //   // var list = Enumerable.Repeat(Data.DescriptorSetLayouts[Layouts.Sampelers], 3).ToArray();
  //   var list = new[] { Data.DescriptorSetLayouts[Layouts.Sampelers] };
  //   var samplerMem = new Memory<DescriptorSetLayout>(list);

  //   var samplerHandle = samplerMem.Pin();
  //   MemoryHandles.Add(samplerHandle);

  //   for (var i = 0; i < LarkVulkanData.MaxFramesInFlight; i++) {
  //     var samplerAllocInfo = new DescriptorSetAllocateInfo {
  //       SType = StructureType.DescriptorSetAllocateInfo,
  //       DescriptorPool = Data.DescriptorPool,
  //       DescriptorSetCount = 1,
  //       PSetLayouts = (DescriptorSetLayout*)samplerHandle.Pointer
  //     };

  //     logger.LogInformation("Allocate sampler descriptor set.");

  //     if (shareData.vk.AllocateDescriptorSets(shareData.Device, &samplerAllocInfo, out SamplerSets.Span[i]) != Result.Success) {
  //       throw new Exception("failed to allocate descriptor sets!");
  //     }
  //   }

  //   // var samplerAllocInfo = new DescriptorSetAllocateInfo {
  //   //   SType = StructureType.DescriptorSetAllocateInfo,
  //   //   DescriptorPool = Data.DescriptorPool,
  //   //   DescriptorSetCount = 1,
  //   //   PSetLayouts = (DescriptorSetLayout*)samplerHandle.Pointer
  //   // };

  //   // logger.LogInformation("Allocate sampler descriptor set.");

  //   // if (shareData.vk.AllocateDescriptorSets(shareData.Device, &samplerAllocInfo, out SamplerSets.Span[0]) != Result.Success) {
  //   //   throw new Exception("failed to allocate descriptor sets!");
  //   // }


  //   // // GBuffers
  //   // var gbufferLayouts = Enumerable.Repeat(Data.DescriptorSetLayouts[Layouts.GBuffers], LarkVulkanData.MaxFramesInFlight).ToArray().AsSpan();
  //   // // var (gbufferLayoutsMem, _) = RegisterMemory(gbufferLayouts);

  //   // var lightLayouts = Enumerable.Repeat(Data.DescriptorSetLayouts[Layouts.LightBuffers], LarkVulkanData.MaxFramesInFlight).ToArray().AsSpan();
  //   // // var (lightLayoutsMem, _) = RegisterMemory(lightLayouts);

  //   // var uiLayouts = Enumerable.Repeat(Data.DescriptorSetLayouts[Layouts.UIBuffers], LarkVulkanData.MaxFramesInFlight).ToArray().AsSpan();
  //   // var (uiLayoutsMem, _) = RegisterMemory(uiLayouts);

  //   // for (var i = 0; i < LarkVulkanData.MaxFramesInFlight; i++) {
  //   //   // var samplerLayouts = Enumerable.Repeat(Data.DescriptorSetLayouts[Layouts.Sampelers], 1).ToArray();
  //   //   // var (samplerLayoutsMem, _) = RegisterMemory(samplerLayouts);

  //   //   var gbufferMem = new Memory<DescriptorSetLayout>([Data.DescriptorSetLayouts[Layouts.LightBuffers]]);
  //   //   var gbufferHandle = gbufferMem.Pin();
  //   //   MemoryHandles.Add(gbufferHandle);

  //   //   var gbufferAllocInfo = new DescriptorSetAllocateInfo {
  //   //     SType = StructureType.DescriptorSetAllocateInfo,
  //   //     DescriptorPool = Data.DescriptorPool,
  //   //     DescriptorSetCount = 1,
  //   //     PSetLayouts = (DescriptorSetLayout*)gbufferHandle.Pointer
  //   //   };

  //   //   logger.LogInformation("Allocate gbuffer descriptor set.");

  //   //   if (shareData.vk.AllocateDescriptorSets(shareData.Device, &gbufferAllocInfo, out GBufferSets.Span[i]) != Result.Success) {
  //   //     throw new Exception("failed to allocate descriptor sets!");
  //   //   }

  //   //   var lightBufferMem = new Memory<DescriptorSetLayout>([Data.DescriptorSetLayouts[Layouts.LightBuffers]]);
  //   //   var lightBufferHandle = lightBufferMem.Pin();
  //   //   MemoryHandles.Add(lightBufferHandle);

  //   //   var lightAllocInfo = new DescriptorSetAllocateInfo {
  //   //     SType = StructureType.DescriptorSetAllocateInfo,
  //   //     DescriptorPool = Data.DescriptorPool,
  //   //     DescriptorSetCount = 1,
  //   //     PSetLayouts = (DescriptorSetLayout*)lightBufferHandle.Pointer
  //   //   };

  //   //   logger.LogInformation("Allocate light descriptor set.");

  //   //   if (shareData.vk.AllocateDescriptorSets(shareData.Device, &lightAllocInfo, out LightSets.Span[i]) != Result.Success) {
  //   //     throw new Exception("failed to allocate descriptor sets!");
  //   //   }

  //   //   var uiBufferMem = new Memory<DescriptorSetLayout>([Data.DescriptorSetLayouts[Layouts.UIBuffers]]);
  //   //   var uiBufferHandle = uiBufferMem.Pin();
  //   //   MemoryHandles.Add(uiBufferHandle);

  //   //   var uiAllocInfo = new DescriptorSetAllocateInfo {
  //   //     SType = StructureType.DescriptorSetAllocateInfo,
  //   //     DescriptorPool = Data.DescriptorPool,
  //   //     DescriptorSetCount = 1,
  //   //     PSetLayouts = (DescriptorSetLayout*)uiBufferHandle.Pointer
  //   //   };

  //   //   logger.LogInformation("Allocate ui descriptor set.");

  //   //   if (shareData.vk.AllocateDescriptorSets(shareData.Device, &uiAllocInfo, out UISets.Span[i]) != Result.Success) {
  //   //     throw new Exception("failed to allocate descriptor sets!");
  //   //   }

  //   //   UpdateDescriptorSets(i);


  //   // }
  // }

  // public unsafe void UpdateDescriptorSets(int frameIndex) {
  //   logger.LogInformation("Update descriptor sets.");

  //   // setup pipeline barriers to ensure that the image is ready to be read from
  //   // var gbufferImageMemoryBarrier = new ImageMemoryBarrier {
  //   //   SType = StructureType.ImageMemoryBarrier,
  //   //   OldLayout = ImageLayout.Undefined,
  //   //   NewLayout = ImageLayout.ShaderReadOnlyOptimal,
  //   //   SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
  //   //   DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
  //   //   Image = geometryPass.Images.Span[frameIndex].Image,
  //   //   SubresourceRange = new ImageSubresourceRange {
  //   //     AspectMask = ImageAspectFlags.ColorBit,
  //   //     BaseMipLevel = 0,
  //   //     LevelCount = 1,
  //   //     BaseArrayLayer = 0,
  //   //     LayerCount = 1
  //   //   },
  //   //   SrcAccessMask = 0,
  //   //   DstAccessMask = AccessFlags.ShaderReadBit
  //   // };

  //   // shareData.vk.CmdPipelineBarrier(
  //   //   shareData.CommandBuffers[frameIndex],
  //   //   PipelineStageFlags.TopOfPipeBit,
  //   //   PipelineStageFlags.FragmentShaderBit,
  //   //   0,
  //   //   0,
  //   //   null,
  //   //   0,
  //   //   null,
  //   //   1,
  //   //   &gbufferImageMemoryBarrier
  //   // );

  //   logger.LogInformation("Update gbuffer descriptor set.");
  //   // update the descriptor sets with the images from the geometry pass and lighting pass
  //   var gbufferImageInfo = new DescriptorImageInfo {
  //     Sampler = geometryPass.Images.Span[frameIndex].Sampler,
  //     ImageView = geometryPass.Images.Span[frameIndex].View,
  //     ImageLayout = ImageLayout.ShaderReadOnlyOptimal
  //   };

  //   var gbufferWrite = new WriteDescriptorSet {
  //     SType = StructureType.WriteDescriptorSet,
  //     DstSet = SamplerSets.Span[frameIndex],
  //     DstBinding = 0,
  //     DstArrayElement = 0,
  //     DescriptorCount = 1,
  //     DescriptorType = DescriptorType.CombinedImageSampler,
  //     PImageInfo = &gbufferImageInfo
  //   };

  //   shareData.vk.UpdateDescriptorSets(shareData.Device, 1, &gbufferWrite, 0, null);

  //   // var lightImageMemoryBarrier = new ImageMemoryBarrier {
  //   //   SType = StructureType.ImageMemoryBarrier,
  //   //   OldLayout = ImageLayout.Undefined,
  //   //   NewLayout = ImageLayout.ShaderReadOnlyOptimal,
  //   //   SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
  //   //   DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
  //   //   Image = lightingPass.Images.Span[frameIndex].Image,
  //   //   SubresourceRange = new ImageSubresourceRange {
  //   //     AspectMask = ImageAspectFlags.ColorBit,
  //   //     BaseMipLevel = 0,
  //   //     LevelCount = 1,
  //   //     BaseArrayLayer = 0,
  //   //     LayerCount = 1
  //   //   },
  //   //   SrcAccessMask = 0,
  //   //   DstAccessMask = AccessFlags.ShaderReadBit
  //   // };

  //   // shareData.vk.CmdPipelineBarrier(
  //   //   shareData.CommandBuffers[frameIndex],
  //   //   PipelineStageFlags.TopOfPipeBit,
  //   //   PipelineStageFlags.FragmentShaderBit,
  //   //   0,
  //   //   0,
  //   //   null,
  //   //   0,
  //   //   null,
  //   //   1,
  //   //   &lightImageMemoryBarrier
  //   // );

  //   logger.LogInformation("Update light descriptor set.");

  //   var lightImageInfo = new DescriptorImageInfo {
  //     Sampler = lightingPass.Images.Span[frameIndex].Sampler,
  //     ImageView = lightingPass.Images.Span[frameIndex].View,
  //     ImageLayout = ImageLayout.ShaderReadOnlyOptimal
  //   };

  //   var lightWrite = new WriteDescriptorSet {
  //     SType = StructureType.WriteDescriptorSet,
  //     DstSet = SamplerSets.Span[frameIndex],
  //     DstBinding = 1,
  //     DstArrayElement = 0,
  //     DescriptorCount = 1,
  //     DescriptorType = DescriptorType.CombinedImageSampler,
  //     PImageInfo = &lightImageInfo
  //   };

  //   shareData.vk.UpdateDescriptorSets(shareData.Device, 1, &lightWrite, 0, null);

  //   // var uiImageMemoryBarrier = new ImageMemoryBarrier {
  //   //   SType = StructureType.ImageMemoryBarrier,
  //   //   OldLayout = ImageLayout.Undefined,
  //   //   NewLayout = ImageLayout.ShaderReadOnlyOptimal,
  //   //   SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
  //   //   DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
  //   //   Image = lightingPass.Images.Span[frameIndex].Image,
  //   //   SubresourceRange = new ImageSubresourceRange {
  //   //     AspectMask = ImageAspectFlags.ColorBit,
  //   //     BaseMipLevel = 0,
  //   //     LevelCount = 1,
  //   //     BaseArrayLayer = 0,
  //   //     LayerCount = 1
  //   //   },
  //   //   SrcAccessMask = 0,
  //   //   DstAccessMask = AccessFlags.ShaderReadBit
  //   // };

  //   // shareData.vk.CmdPipelineBarrier(
  //   //   shareData.CommandBuffers[frameIndex],
  //   //   PipelineStageFlags.TopOfPipeBit,
  //   //   PipelineStageFlags.FragmentShaderBit,
  //   //   0,
  //   //   0,
  //   //   null,
  //   //   0,
  //   //   null,
  //   //   1,
  //   //   &uiImageMemoryBarrier
  //   // );

  //   logger.LogInformation("Update ui descriptor set.");

  //   var uiImageInfo = new DescriptorImageInfo {
  //     Sampler = uiPass.Images.Span[frameIndex].Sampler,
  //     ImageView = uiPass.Images.Span[frameIndex].View,
  //     ImageLayout = ImageLayout.ShaderReadOnlyOptimal
  //   };

  //   var uiWrite = new WriteDescriptorSet {
  //     SType = StructureType.WriteDescriptorSet,
  //     DstSet = SamplerSets.Span[frameIndex],
  //     DstBinding = 2,
  //     DstArrayElement = 0,
  //     DescriptorCount = 1,
  //     DescriptorType = DescriptorType.CombinedImageSampler,
  //     PImageInfo = &uiImageInfo
  //   };

  //   shareData.vk.UpdateDescriptorSets(shareData.Device, 1, &uiWrite, 0, null);
  // }

  // public unsafe void BindDescriptorSets(int frameIndex) {
  //   logger.LogInformation("Bind descriptor sets.");

  //   // // var sets = new[] { GBufferSets.Span[frameIndex], LightSets.Span[frameIndex], UISets.Span[frameIndex] };
  //   // var (setsMem, setsSize) = RegisterMemory(sets);

  //   var handle = SamplerSets.Pin();

  //   // bind the descriptor sets to the pipeline from the geometry pass and lighting pass
  //   shareData.vk.CmdBindDescriptorSets(
  //     shareData.CommandBuffers[frameIndex],
  //     PipelineBindPoint.Graphics,
  //     Data.PipelineLayout,
  //     0,
  //     1,
  //     (DescriptorSet*)handle.Pointer,
  //     0,
  //     null
  //   );

  //   handle.Dispose();

  //   // shareData.vk.CmdBindDescriptorSets(
  //   //   shareData.CommandBuffers[frameIndex],
  //   //   PipelineBindPoint.Graphics,
  //   //   Data.PipelineLayout,
  //   //   0,
  //   //   1,
  //   //   LightSets.Span[frameIndex],
  //   //   0,
  //   //   null
  //   // );

  //   // shareData.vk.CmdBindDescriptorSets(
  //   //   shareData.CommandBuffers[frameIndex],
  //   //   PipelineBindPoint.Graphics,
  //   //   Data.PipelineLayout,
  //   //   0,
  //   //   1,
  //   //   UISets.Span[frameIndex],
  //   //   0,
  //   //   null
  //   // );
  // }

  // public unsafe void CreateDescriptorSetLayouts() {
  //   // we will pass in 3 sets. The GBuffers, the LightBuffers, and the UIBuffers.
  //   // The composite pipeline will merge all of these into the final image in the framebuffer tied to the swapchain.

  //   var gbufferLayoutBinding = new DescriptorSetLayoutBinding {
  //     Binding = 0,
  //     DescriptorType = DescriptorType.CombinedImageSampler,
  //     DescriptorCount = 1,
  //     StageFlags = ShaderStageFlags.FragmentBit
  //   };
  //   var lightLayoutBinding = new DescriptorSetLayoutBinding {
  //     Binding = 1,
  //     DescriptorType = DescriptorType.CombinedImageSampler,
  //     DescriptorCount = 1,
  //     StageFlags = ShaderStageFlags.FragmentBit
  //   };
  //   var uiLayoutBinding = new DescriptorSetLayoutBinding {
  //     Binding = 2,
  //     DescriptorType = DescriptorType.CombinedImageSampler,
  //     DescriptorCount = 1,
  //     StageFlags = ShaderStageFlags.FragmentBit
  //   };

  //   var layouts = new[] { gbufferLayoutBinding, lightLayoutBinding, uiLayoutBinding };
  //   var (layoutsMem, layoutsSize) = RegisterMemory(layouts);

  //   var samplerLayoutCreateInfo = new DescriptorSetLayoutCreateInfo {
  //     SType = StructureType.DescriptorSetLayoutCreateInfo,
  //     BindingCount = layoutsSize,
  //     PBindings = (DescriptorSetLayoutBinding*)layoutsMem.Pointer
  //   };

  //   if (shareData.vk.CreateDescriptorSetLayout(shareData.Device, &samplerLayoutCreateInfo, null, out var samplerLayout) != Result.Success) {
  //     throw new Exception("failed to create descriptor set layout!");
  //   }

  //   Data.DescriptorSetLayouts.Add(Layouts.Sampelers, samplerLayout);

  //   // GBuffers
  //   // var gbufferLayoutBinding = new DescriptorSetLayoutBinding {
  //   //   Binding = 0,
  //   //   DescriptorType = DescriptorType.CombinedImageSampler,
  //   //   DescriptorCount = LarkVulkanData.MaxFramesInFlight,
  //   //   StageFlags = ShaderStageFlags.FragmentBit
  //   // };

  //   // var gbufferLayoutCreateInfo = new DescriptorSetLayoutCreateInfo {
  //   //   SType = StructureType.DescriptorSetLayoutCreateInfo,
  //   //   BindingCount = 1,
  //   //   PBindings = &gbufferLayoutBinding
  //   // };

  //   // if (shareData.vk.CreateDescriptorSetLayout(shareData.Device, &gbufferLayoutCreateInfo, null, out var gbufferLayout) != Result.Success) {
  //   //   throw new Exception("failed to create descriptor set layout!");
  //   // }

  //   // Data.DescriptorSetLayouts.Add(Layouts.GBuffers, gbufferLayout);

  //   // // LightBuffers
  //   // var lightLayoutBinding = new DescriptorSetLayoutBinding {
  //   //   Binding = 1,
  //   //   DescriptorType = DescriptorType.CombinedImageSampler,
  //   //   DescriptorCount = LarkVulkanData.MaxFramesInFlight,
  //   //   StageFlags = ShaderStageFlags.FragmentBit
  //   // };

  //   // var lightLayoutCreateInfo = new DescriptorSetLayoutCreateInfo {
  //   //   SType = StructureType.DescriptorSetLayoutCreateInfo,
  //   //   BindingCount = 1,
  //   //   PBindings = &lightLayoutBinding
  //   // };

  //   // if (shareData.vk.CreateDescriptorSetLayout(shareData.Device, &lightLayoutCreateInfo, null, out var lightLayout) != Result.Success) {
  //   //   throw new Exception("failed to create descriptor set layout!");
  //   // }

  //   // Data.DescriptorSetLayouts.Add(Layouts.LightBuffers, lightLayout);

  //   // // UIBuffers
  //   // var uiLayoutBinding = new DescriptorSetLayoutBinding {
  //   //   Binding = 2,
  //   //   DescriptorType = DescriptorType.CombinedImageSampler,
  //   //   DescriptorCount = LarkVulkanData.MaxFramesInFlight,
  //   //   StageFlags = ShaderStageFlags.FragmentBit
  //   // };

  //   // var uiLayoutCreateInfo = new DescriptorSetLayoutCreateInfo {
  //   //   SType = StructureType.DescriptorSetLayoutCreateInfo,
  //   //   BindingCount = 1,
  //   //   PBindings = &uiLayoutBinding
  //   // };

  //   // if (shareData.vk.CreateDescriptorSetLayout(shareData.Device, &uiLayoutCreateInfo, null, out var uiLayout) != Result.Success) {
  //   //   throw new Exception("failed to create descriptor set layout!");
  //   // }

  //   // Data.DescriptorSetLayouts.Add(Layouts.UIBuffers, uiLayout);
  // }

  public unsafe void CreateDescriptorPool() {
    uint setCount = LarkVulkanData.MaxFramesInFlight * 3; // 3 total layouts
    var poolSizes = new DescriptorPoolSize[] {
      new DescriptorPoolSize {
        Type = DescriptorType.CombinedImageSampler,
        DescriptorCount = setCount
      }
    };

    var (poolSizesMem, _) = RegisterMemory(poolSizes);

    var poolCreateInfo = new DescriptorPoolCreateInfo {
      SType = StructureType.DescriptorPoolCreateInfo,
      PoolSizeCount = 1,
      PPoolSizes = (DescriptorPoolSize*)poolSizesMem.Pointer,
      MaxSets = setCount
    };

    if (shareData.vk.CreateDescriptorPool(shareData.Device, &poolCreateInfo, null, out Data.DescriptorPool) != Result.Success) {
      throw new Exception("failed to create descriptor pool!");
    }
  }

  public unsafe void CreateRenderPass() {
    var colorAttachment = new AttachmentDescription {
      Format = shareData.SwapchainImageFormat,
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

    var gbufferAttachment = new AttachmentDescription {
      Format = Format.B8G8R8A8Unorm,
      Samples = SampleCountFlags.Count1Bit,
      LoadOp = AttachmentLoadOp.DontCare,
      StoreOp = AttachmentStoreOp.Store,
      StencilLoadOp = AttachmentLoadOp.DontCare,
      StencilStoreOp = AttachmentStoreOp.DontCare,
      InitialLayout = ImageLayout.Undefined,
      FinalLayout = ImageLayout.ShaderReadOnlyOptimal
    };

    var gbufferAttachmentRef = new AttachmentReference {
      Attachment = 1,
      Layout = ImageLayout.ShaderReadOnlyOptimal
    };

    var lightBufferAttachment = new AttachmentDescription {
      Format = Format.B8G8R8A8Unorm,
      Samples = SampleCountFlags.Count1Bit,
      LoadOp = AttachmentLoadOp.DontCare,
      StoreOp = AttachmentStoreOp.Store,
      StencilLoadOp = AttachmentLoadOp.DontCare,
      StencilStoreOp = AttachmentStoreOp.DontCare,
      InitialLayout = ImageLayout.Undefined,
      FinalLayout = ImageLayout.ShaderReadOnlyOptimal
    };

    var lightBufferAttachmentRef = new AttachmentReference {
      Attachment = 2,
      Layout = ImageLayout.ShaderReadOnlyOptimal
    };

    var uiBufferAttachment = new AttachmentDescription {
      Format = Format.B8G8R8A8Unorm,
      Samples = SampleCountFlags.Count1Bit,
      LoadOp = AttachmentLoadOp.DontCare,
      StoreOp = AttachmentStoreOp.Store,
      StencilLoadOp = AttachmentLoadOp.DontCare,
      StencilStoreOp = AttachmentStoreOp.DontCare,
      InitialLayout = ImageLayout.Undefined,
      FinalLayout = ImageLayout.ShaderReadOnlyOptimal
    };

    var uiBufferAttachmentRef = new AttachmentReference {
      Attachment = 3,
      Layout = ImageLayout.ShaderReadOnlyOptimal
    };

    var inputAttachments = new AttachmentReference[] {
      gbufferAttachmentRef,
      lightBufferAttachmentRef,
      uiBufferAttachmentRef
    };

    var (inputAttachmentsMem, inputAttachmentSize) = RegisterMemory(inputAttachments);

    var subpass = new SubpassDescription {
      PipelineBindPoint = PipelineBindPoint.Graphics,
      ColorAttachmentCount = 1,
      PColorAttachments = &colorAttachmentRef,
      InputAttachmentCount = inputAttachmentSize,
      PInputAttachments = (AttachmentReference*)inputAttachmentsMem.Pointer
    };

    var dependency = new SubpassDependency {
      SrcSubpass = Vk.SubpassExternal,
      DstSubpass = 0,
      SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
      SrcAccessMask = 0,
      DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
      DstAccessMask = AccessFlags.ColorAttachmentReadBit | AccessFlags.ColorAttachmentWriteBit
    };
    var attachments = new AttachmentDescription[] {
      colorAttachment,
      gbufferAttachment,
      lightBufferAttachment,
      uiBufferAttachment
    };

    var (attachmentMem, attachmentSize) = RegisterMemory(attachments);

    var dependencies = new SubpassDependency[] {
      dependency,
    };

    var (dependencyMem, dependencySize) = RegisterMemory(dependencies);

    var renderPassCreateInfo = new RenderPassCreateInfo {
      SType = StructureType.RenderPassCreateInfo,
      AttachmentCount = attachmentSize,
      PAttachments = (AttachmentDescription*)attachmentMem.Pointer,
      SubpassCount = 1,
      PSubpasses = &subpass,
      DependencyCount = dependencySize,
      PDependencies = (SubpassDependency*)dependencyMem.Pointer
    };

    if (shareData.vk.CreateRenderPass(shareData.Device, &renderPassCreateInfo, null, out Data.RenderPass) != Result.Success) {
      throw new Exception("failed to create render pass!");
    }
  }

  public unsafe void CreateGraphicsPipeline() {
    // Create Composite pipeline

    var vertShaderInfo = shaderBuilder.LoadShader("composite.vert");
    var fragShaderInfo = shaderBuilder.LoadShader("composite.frag");

    var vertShaderModule = CreateShaderModule(vertShaderInfo);
    var fragShaderModule = CreateShaderModule(fragShaderInfo);

    var vertShaderStageInfo = new PipelineShaderStageCreateInfo {
      SType = StructureType.PipelineShaderStageCreateInfo,
      Stage = ShaderStageFlags.VertexBit,
      Module = vertShaderModule,
      PName = (byte*)SilkMarshal.StringToPtr("main")
    };

    var fragShaderStageInfo = new PipelineShaderStageCreateInfo {
      SType = StructureType.PipelineShaderStageCreateInfo,
      Stage = ShaderStageFlags.FragmentBit,
      Module = fragShaderModule,
      PName = (byte*)SilkMarshal.StringToPtr("main")
    };

    var shaderStages = stackalloc[] { vertShaderStageInfo, fragShaderStageInfo };

    // var layouts = new[] { Data.DescriptorSetLayouts[Layouts.GBuffers], Data.DescriptorSetLayouts[Layouts.LightBuffers], Data.DescriptorSetLayouts[Layouts.UIBuffers] };
    // var layouts = new[] { Data.DescriptorSetLayouts[Layouts.Sampelers] };
    // Don't think we would have the usual vertex bindings here given we just have a quad

    var layouts = Data.PipelineSets.Values.Select(s => s.Layout).ToArray();
    var (setLayoutsMem, setLayoutsSize) = RegisterMemory(layouts);

    var viewport = new Viewport {
      X = 0.0f,
      Y = 0.0f,
      Width = shareData.SwapchainExtent.Width,
      Height = shareData.SwapchainExtent.Height,
      MinDepth = 0.0f,
      MaxDepth = 1.0f
    };

    var scissor = new Rect2D { Offset = default, Extent = shareData.SwapchainExtent };

    var viewportState = new PipelineViewportStateCreateInfo {
      SType = StructureType.PipelineViewportStateCreateInfo,
      ViewportCount = 1,
      PViewports = &viewport,
      ScissorCount = 1,
      PScissors = &scissor
    };

    var rasterizer = new PipelineRasterizationStateCreateInfo {
      SType = StructureType.PipelineRasterizationStateCreateInfo,
      DepthClampEnable = Vk.False,
      RasterizerDiscardEnable = Vk.False,
      PolygonMode = PolygonMode.Fill,
      LineWidth = 1.0f,
      CullMode = CullModeFlags.BackBit,
      FrontFace = FrontFace.Clockwise,
      DepthBiasEnable = Vk.False
    };

    var multisampling = new PipelineMultisampleStateCreateInfo {
      SType = StructureType.PipelineMultisampleStateCreateInfo,
      SampleShadingEnable = Vk.False,
      RasterizationSamples = SampleCountFlags.Count1Bit
    };

    var colorBlendAttachment = new PipelineColorBlendAttachmentState {
      ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
      BlendEnable = Vk.True,
      SrcColorBlendFactor = BlendFactor.SrcAlpha,
      DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha,
      ColorBlendOp = BlendOp.Add,
      SrcAlphaBlendFactor = BlendFactor.One,
      DstAlphaBlendFactor = BlendFactor.Zero,
      AlphaBlendOp = BlendOp.Add
    };

    var colorBlending = new PipelineColorBlendStateCreateInfo {
      SType = StructureType.PipelineColorBlendStateCreateInfo,
      LogicOpEnable = Vk.False,
      LogicOp = LogicOp.Copy,
      AttachmentCount = 1,
      PAttachments = &colorBlendAttachment
    };

    colorBlending.BlendConstants[0] = 0.0f;
    colorBlending.BlendConstants[1] = 0.0f;
    colorBlending.BlendConstants[2] = 0.0f;
    colorBlending.BlendConstants[3] = 0.0f;

    var dynamicStates = stackalloc[] { DynamicState.Viewport, DynamicState.LineWidth };

    var dynamicState = new PipelineDynamicStateCreateInfo {
      SType = StructureType.PipelineDynamicStateCreateInfo,
      DynamicStateCount = 2,
      PDynamicStates = dynamicStates
    };

    var pipelineLayoutInfo = new PipelineLayoutCreateInfo {
      SType = StructureType.PipelineLayoutCreateInfo,
      SetLayoutCount = setLayoutsSize,
      PSetLayouts = (DescriptorSetLayout*)setLayoutsMem.Pointer,
      PushConstantRangeCount = 0,
      PPushConstantRanges = null
    };

    if (shareData.vk.CreatePipelineLayout(shareData.Device, &pipelineLayoutInfo, null, out Data.PipelineLayout) != Result.Success) {
      throw new Exception("failed to create pipeline layout!");
    }

    var vertexInputInfo = new PipelineVertexInputStateCreateInfo {
      SType = StructureType.PipelineVertexInputStateCreateInfo,
      VertexBindingDescriptionCount = 0,
      VertexAttributeDescriptionCount = 0
    };

    var inputAssembly = new PipelineInputAssemblyStateCreateInfo {
      SType = StructureType.PipelineInputAssemblyStateCreateInfo,
      Topology = PrimitiveTopology.TriangleList,
      PrimitiveRestartEnable = Vk.False
    };

    var pipelineInfo = new GraphicsPipelineCreateInfo {
      SType = StructureType.GraphicsPipelineCreateInfo,
      StageCount = 2,
      PStages = shaderStages,
      PViewportState = &viewportState,
      PRasterizationState = &rasterizer,
      PMultisampleState = &multisampling,
      PDepthStencilState = null,
      PColorBlendState = &colorBlending,
      PDynamicState = &dynamicState,
      Layout = Data.PipelineLayout,
      PVertexInputState = &vertexInputInfo,
      PInputAssemblyState = &inputAssembly,
      RenderPass = Data.RenderPass,
      Subpass = 0,
      BasePipelineHandle = default
    };

    if (shareData.vk.CreateGraphicsPipelines(shareData.Device, default, 1, &pipelineInfo, null, out Data.Pipeline) != Result.Success) {
      throw new Exception("failed to create graphics pipeline!");
    }

    shareData.vk.DestroyShaderModule(shareData.Device, vertShaderModule, null);
    shareData.vk.DestroyShaderModule(shareData.Device, fragShaderModule, null);

    logger.LogInformation("Created Ultralight pipeline.");

  }
}