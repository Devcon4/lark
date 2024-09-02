using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lark.Engine.model;
using Lark.Engine.std;
using Microsoft.Extensions.Logging;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace Lark.Engine.pipeline;


public class LightingPassData() {
  public Memory<LarkImage> Images = new(new LarkImage[LarkVulkanData.MaxFramesInFlight]);
}

public class LightingPipeline(ILogger<LightingPipeline> logger, LightingPassData lightingPass, GeometryPassData geometryPass, LarkVulkanData shareData, BufferUtils bufferUtils, ImageUtils imageUtils, CameraManager cm, ShaderBuilder shaderBuilder) : LarkPipeline(shareData) {

  public readonly ulong LarkLightShaderSize = 64; // Make sure this matches the size of the shader struct. Must be a multiple of 16.
  public const ulong DefaultMaxLights = 128;
  private Memory<DescriptorSet> LightDescriptorSet = new DescriptorSet[DefaultMaxLights * LarkVulkanData.MaxFramesInFlight];
  private Memory<DescriptorSet> SamplerSets = new DescriptorSet[LarkVulkanData.MaxFramesInFlight];

  public LarkBuffer LightDataBuffer;

  public struct Layouts() {
    public static string GBuffers = "GBuffers";
    // public static string GBufferNormals = "GBufferNormals";
    public static string Lights = "Lights";
  }

  public override void Start() {
    CreateDescriptorPool();
    DeclarePipelineSets();
    CreateSetLayouts();

    // CreateDescriptorSetLayouts();
    CreateLightingRenderPass();
    CreateLightBufferImages();
    CreateLightDataBuffer();
    CreateFramebuffers();
    CreateSet(Layouts.GBuffers);
    CreateSet(Layouts.Lights); //, (int)DefaultMaxLights * LarkVulkanData.MaxFramesInFlight

    CreateGraphicsPipeline();
    CreateClearValues();
    // CreateDescriptorSets();
    logger.LogInformation("Lighting pipeline finished.");
  }

  public override void Update(uint index) {
    UpdateAllLightData();
    // foreach (var light in shareData.lights.Values) {
    //   UpdateLightData(light, (int)index);
    // }
  }

  private void DeclarePipelineSets() {
    RegisterSet(Layouts.GBuffers, 0, [
      new LarkLayoutBindingInfo(DescriptorType.CombinedImageSampler, ShaderStageFlags.FragmentBit, 0),
      new LarkLayoutBindingInfo(DescriptorType.CombinedImageSampler, ShaderStageFlags.FragmentBit, 1),
      new LarkLayoutBindingInfo(DescriptorType.CombinedImageSampler, ShaderStageFlags.FragmentBit, 2)
    ]);

    RegisterSet(Layouts.Lights, 1, [
      new LarkLayoutBindingInfo(DescriptorType.StorageBuffer, ShaderStageFlags.FragmentBit, 0)
    ]);
  }

  private unsafe void UpdateGBufferSets(int index) {
    var geoImageInfo = new DescriptorImageInfo {
      Sampler = geometryPass.Images.Span[index].Sampler,
      ImageView = geometryPass.Images.Span[index].View,
      ImageLayout = ImageLayout.ShaderReadOnlyOptimal
    };

    UpdateSet(Layouts.GBuffers, (uint)index, new WriteDescriptorSet {
      SType = StructureType.WriteDescriptorSet,
      DstBinding = 0,
      DescriptorCount = 1,
      DescriptorType = DescriptorType.CombinedImageSampler,
      PImageInfo = &geoImageInfo
    });

    var normalImageInfo = new DescriptorImageInfo {
      Sampler = geometryPass.Normals.Span[index].Sampler,
      ImageView = geometryPass.Normals.Span[index].View,
      ImageLayout = ImageLayout.ShaderReadOnlyOptimal
    };

    UpdateSet(Layouts.GBuffers, (uint)index, new WriteDescriptorSet {
      SType = StructureType.WriteDescriptorSet,
      DstBinding = 1,
      DescriptorCount = 1,
      DescriptorType = DescriptorType.CombinedImageSampler,
      PImageInfo = &normalImageInfo
    });

    var depthImageInfo = new DescriptorImageInfo {
      Sampler = geometryPass.Depth.Span[index].Sampler,
      ImageView = geometryPass.Depth.Span[index].View,
      ImageLayout = ImageLayout.ShaderReadOnlyOptimal
    };

    UpdateSet(Layouts.GBuffers, (uint)index, new WriteDescriptorSet {
      SType = StructureType.WriteDescriptorSet,
      DstBinding = 2,
      DescriptorCount = 1,
      DescriptorType = DescriptorType.CombinedImageSampler,
      PImageInfo = &depthImageInfo
    });
  }

  // private unsafe void UpdateLightDescriptorSet(int frameIndex, int lightIndex) {
  //   var lightOffset = LarkLightShaderSize * (ulong)lightIndex;

  //   var lightBufferInfo = new DescriptorBufferInfo {
  //     Buffer = LightDataBuffer.Buffer,
  //     Offset = lightOffset,
  //     Range = LarkLightShaderSize
  //   };

  //   var bufferOffset = lightIndex + (frameIndex * (int)DefaultMaxLights);

  //   var lightBufferDescriptor = new WriteDescriptorSet {
  //     SType = StructureType.WriteDescriptorSet,
  //     DstSet = LightDescriptorSet.Span[bufferOffset],
  //     DstBinding = 0,
  //     DstArrayElement = 0,
  //     DescriptorCount = 1,
  //     DescriptorType = DescriptorType.StorageBuffer,
  //     PBufferInfo = &lightBufferInfo
  //   };

  //   shareData.vk.UpdateDescriptorSets(shareData.Device, 1, &lightBufferDescriptor, 0, null);
  // }
  private unsafe void UpdateLightSet(int frameIndex) {
    // var lightOffset = (ulong)lightIndex * LarkLightShaderSize;

    var lightBufferInfo = new DescriptorBufferInfo {
      Buffer = LightDataBuffer.Buffer,
      Offset = 0,
      Range = LarkLightShaderSize * DefaultMaxLights
    };

    // var bufferOffset = lightIndex + (frameIndex * (int)DefaultMaxLights);

    UpdateSet(Layouts.Lights, (uint)frameIndex, new WriteDescriptorSet {
      SType = StructureType.WriteDescriptorSet,
      DstBinding = 0,
      DescriptorCount = 1,
      DstArrayElement = 0,
      DescriptorType = DescriptorType.StorageBuffer,
      PBufferInfo = &lightBufferInfo
    });
  }
  private int currLight = 0;
  public override void Draw(uint index) {
    logger.LogInformation("Draw light pipeline {index}", index);

    // TransitionGBufferImages((int)index, ImageLayout.ShaderReadOnlyOptimal);

    imageUtils.TransitionImageLayout(geometryPass.Images.Span[(int)index].Image, ImageLayout.ShaderReadOnlyOptimal, ImageLayout.ColorAttachmentOptimal, ImageAspectFlags.ColorBit);
    imageUtils.TransitionImageLayout(geometryPass.Normals.Span[(int)index].Image, ImageLayout.ShaderReadOnlyOptimal, ImageLayout.ColorAttachmentOptimal, ImageAspectFlags.ColorBit);
    imageUtils.TransitionImageLayout(geometryPass.Depth.Span[(int)index].Image, ImageLayout.ShaderReadOnlyOptimal, ImageLayout.DepthStencilAttachmentOptimal, ImageAspectFlags.DepthBit);

    geometryPass.Images.Span[(int)index].Layout = ImageLayout.ShaderReadOnlyOptimal;
    geometryPass.Normals.Span[(int)index].Layout = ImageLayout.ShaderReadOnlyOptimal;
    geometryPass.Depth.Span[(int)index].Layout = ImageLayout.ShaderReadOnlyOptimal;

    // UpdateGBufferDescriptorSets((int)index);
    // BindGBufferDescriptorSets((int)index);
    UpdateGBufferSets((int)index);
    UpdateLightSet((int)index);

    BindSet(Layouts.GBuffers, index);
    BindSet(Layouts.Lights, index);

    logger.LogInformation("Drawing {count} lights.", shareData.lights.Count);
    logger.LogInformation("Current light index: {currLight}", currLight);

    UpdatePushConstants(index, currLight);
    shareData.vk.CmdDraw(shareData.CommandBuffers[index], 6, 1, 0, 0);

    // for (var lightIndex = 0; lightIndex < shareData.lights.Count; lightIndex++) {
    //   UpdatePushConstants(index, lightIndex);

    //   if (currLight != lightIndex) {
    //     continue;
    //   }

    //   logger.LogInformation("Light color: {color}", shareData.lights.Values.ToArray()[lightIndex].Settings.Color);
    //   int lightDescriptorIndex = lightIndex + (int)(index * DefaultMaxLights);
    //   UpdateLightSet((int)index, lightIndex);
    //   BindSet(Layouts.Lights, index, lightDescriptorIndex, 1); // Need to check if index is right or if we need to use lightDescriptorIndex.
    //   shareData.vk.CmdDraw(shareData.CommandBuffers[index], 6, 1, 0, 0);
    //   // DrawLight(index, lightIndex);
    // }

    currLight++;

    if (currLight > shareData.lights.Count - 1) {
      currLight = 0;
    }
  }

  private void UpdateGbufferImageLayout(int index) {
    geometryPass.Images.Span[index].Layout = ImageLayout.ColorAttachmentOptimal;
    geometryPass.Normals.Span[index].Layout = ImageLayout.ColorAttachmentOptimal;
    geometryPass.Depth.Span[index].Layout = ImageLayout.DepthStencilAttachmentOptimal;
  }

  private unsafe void UpdatePushConstants(uint index, int lightIndex) {
    if (cm.ActiveCamera is null) {
      logger.LogWarning("No active camera found; skipping push constants.");
      return;
    }

    logger.LogInformation("Updating push constants...");

    var cameraConstants = new LarkCameraConstants {
      InvertView = cm.ActiveCamera.Value.InvertView.ToGeneric(),
      InvertProjection = cm.ActiveCamera.Value.InvertProjection.ToGeneric(),
      CameraPosition = new Vector4D<float>(cm.ActiveCamera.Value.Transform.Translation, 1.0f),
      lightIndex = lightIndex
    };
    shareData.vk.CmdPushConstants(shareData.CommandBuffers[index], Data.PipelineLayout, ShaderStageFlags.FragmentBit, 0, (uint)Marshal.SizeOf<LarkCameraConstants>(), &cameraConstants);
  }

  private void TransitionGBufferImages(int index, ImageLayout newLayout) {
    imageUtils.TransitionImageLayout(ref geometryPass.Images.Span[index], newLayout);
    imageUtils.TransitionImageLayout(ref geometryPass.Normals.Span[index], newLayout);
    imageUtils.TransitionImageLayout(ref geometryPass.Depth.Span[index], newLayout, ImageAspectFlags.DepthBit);
  }

  // private unsafe void BindGBufferDescriptorSets(int frameIndex) {
  //   var handle = SamplerSets.Pin();

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
  // }


  public unsafe override void Cleanup() {
    shareData.vk.DestroyDescriptorPool(shareData.Device, Data.DescriptorPool, null);

    foreach (var layout in Data.DescriptorSetLayouts.Values) {
      shareData.vk.DestroyDescriptorSetLayout(shareData.Device, layout, null);
    }

    foreach (var image in lightingPass.Images.Span) {
      image.Dispose(shareData);
    }

    foreach (var framebuffer in Data.Framebuffers) {
      shareData.vk.DestroyFramebuffer(shareData.Device, framebuffer, null);
    }

    LightDataBuffer.Dispose(shareData);

    shareData.vk.DestroyPipeline(shareData.Device, Data.Pipeline, null);
    shareData.vk.DestroyRenderPass(shareData.Device, Data.RenderPass, null);
    shareData.vk.DestroyPipelineLayout(shareData.Device, Data.PipelineLayout, null);
  }
  public unsafe void CreateFramebuffers() {
    Data.Framebuffers = new Framebuffer[LarkVulkanData.MaxFramesInFlight];

    for (var i = 0; i < LarkVulkanData.MaxFramesInFlight; i++) {
      var attachments = new ImageView[] {
        lightingPass.Images.Span[i].View
      };

      var (attachmentsMem, _) = RegisterMemory(attachments);

      var framebufferCreateInfo = new FramebufferCreateInfo {
        SType = StructureType.FramebufferCreateInfo,
        RenderPass = Data.RenderPass,
        AttachmentCount = 1,
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

  public void CreateLightDataBuffer() { // TODO: currently hardcoded to 128 lights. Make it so we can resize the buffer in the future.
    var bufferSize = LarkLightShaderSize * DefaultMaxLights; // 128 lights

    bufferUtils.CreateBuffer(
      bufferSize,
      new BufferAllocInfo {
        Usage = BufferUsageFlags.StorageBufferBit,
        Properties = MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
        SharingMode = SharingMode.Exclusive
      },
      ref LightDataBuffer
    );
  }

  public unsafe void CreateLightBufferImages() {
    for (int i = 0; i < LarkVulkanData.MaxFramesInFlight; i++) {
      imageUtils.CreateImage(shareData.SwapchainExtent.Width, shareData.SwapchainExtent.Height, Format.B8G8R8A8Unorm, ImageTiling.Optimal, ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.SampledBit | ImageUsageFlags.InputAttachmentBit, MemoryPropertyFlags.DeviceLocalBit, ref lightingPass.Images.Span[i].Image, ref lightingPass.Images.Span[i].Memory);
      lightingPass.Images.Span[i].View = imageUtils.CreateImageView(lightingPass.Images.Span[i].Image, Format.B8G8R8A8Unorm, ImageAspectFlags.ColorBit);
      // CreateLightBufferSampler(ref lightingPass.Images.Span[i].Sampler);
      imageUtils.CreateSampler(ref lightingPass.Images.Span[i].Sampler);
    }
  }

  // private unsafe void CreateLightBufferSampler(ref Sampler LightSampler) {
  //   var samplerInfo = new SamplerCreateInfo {
  //     SType = StructureType.SamplerCreateInfo,
  //     MagFilter = Filter.Linear,
  //     MinFilter = Filter.Linear,
  //     AddressModeU = SamplerAddressMode.ClampToEdge,
  //     AddressModeV = SamplerAddressMode.ClampToEdge,
  //     AddressModeW = SamplerAddressMode.ClampToEdge,
  //     AnisotropyEnable = Vk.False,
  //     BorderColor = BorderColor.IntOpaqueBlack,
  //     UnnormalizedCoordinates = Vk.False,
  //     CompareEnable = Vk.False,
  //     CompareOp = CompareOp.Always,
  //     MipmapMode = SamplerMipmapMode.Linear
  //   };

  //   if (shareData.vk.CreateSampler(shareData.Device, &samplerInfo, null, out LightSampler) != Result.Success) {
  //     throw new Exception("failed to create texture sampler!");
  //   }
  // }

  // public unsafe void CreateDescriptorSets() {
  //   var layouts = new[] {
  //     Data.DescriptorSetLayouts[Layouts.GBuffers],
  //     Data.DescriptorSetLayouts[Layouts.Lights]
  //   };

  //   var layoutsMem = new Memory<DescriptorSetLayout>(layouts);
  //   var layoutHandle = layoutsMem.Pin();

  //   for (var frameIndex = 0; frameIndex < LarkVulkanData.MaxFramesInFlight; frameIndex++) {

  //     var gbufferAllocInfo = new DescriptorSetAllocateInfo {
  //       SType = StructureType.DescriptorSetAllocateInfo,
  //       DescriptorPool = Data.DescriptorPool,
  //       DescriptorSetCount = 2, // geo and normal
  //       PSetLayouts = (DescriptorSetLayout*)layoutHandle.Pointer
  //     };

  //     if (shareData.vk.AllocateDescriptorSets(shareData.Device, &gbufferAllocInfo, out SamplerSets.Span[frameIndex]) != Result.Success) {
  //       throw new Exception("failed to allocate descriptor sets!");
  //     }

  //     UpdateGBufferDescriptorSets(frameIndex);

  //     for (var lightIndex = 0; lightIndex < (uint)DefaultMaxLights; lightIndex++) {
  //       var lightAllocInfo = new DescriptorSetAllocateInfo {
  //         SType = StructureType.DescriptorSetAllocateInfo,
  //         DescriptorPool = Data.DescriptorPool,
  //         DescriptorSetCount = 1,
  //         PSetLayouts = (DescriptorSetLayout*)layoutHandle.Pointer + 1 // lights
  //       };

  //       var setOffset = (int)lightIndex + (frameIndex * (int)DefaultMaxLights);
  //       logger.LogInformation("Allocating descriptor set {setOffset} for light {lightIndex} at frame {frameIndex}", setOffset, lightIndex, frameIndex);
  //       if (shareData.vk.AllocateDescriptorSets(shareData.Device, &lightAllocInfo, out LightDescriptorSet.Span[setOffset]) != Result.Success) {
  //         throw new Exception("failed to allocate descriptor sets!");
  //       }

  //       UpdateLightDescriptorSet(frameIndex, lightIndex);
  //     }
  //   }

  //   layoutHandle.Dispose();
  // }

  // public override void Update(uint frameIndex) {
  //   UpdateDescriptorSets((int)frameIndex);
  // }

  // public unsafe void DrawLight(uint frameIndex, int lightIndex) {
  //   var lightDescriptorIndex = lightIndex + (frameIndex * (int)DefaultMaxLights);
  //   logger.LogInformation("Binding light descriptor set {lightDescriptorIndex} for light {lightIndex} at frame {frameIndex}", lightDescriptorIndex, lightIndex, frameIndex);
  //   shareData.vk.CmdBindDescriptorSets(
  //     shareData.CommandBuffers[frameIndex],
  //     PipelineBindPoint.Graphics,
  //     Data.PipelineLayout,
  //     1,
  //     1,
  //     LightDescriptorSet.Span[(int)lightDescriptorIndex],
  //     0,
  //     null
  //   );

  //   shareData.vk.CmdDraw(shareData.CommandBuffers[frameIndex], 6, 1, 0, 0); // TODO: check this.
  // }

  public unsafe void UpdateLightData(LarkLight light, int index) {
    var offset = (int)LarkLightShaderSize * index;

    void* dataPtr;
    shareData.vk.MapMemory(shareData.Device, LightDataBuffer.Memory, (uint)offset, LarkLightShaderSize, 0, &dataPtr);

    Marshal.StructureToPtr(light.ToShader(), (nint)dataPtr, false);

    shareData.vk.UnmapMemory(shareData.Device, LightDataBuffer.Memory);
  }

  public unsafe void UpdateAllLightData() {
    void* dataPtr;
    var mem = new Memory<LarkLightShader>(shareData.lights.Values.Select(l => l.ToShader()).ToArray());

    shareData.vk.MapMemory(shareData.Device, LightDataBuffer.Memory, 0, LarkLightShaderSize * DefaultMaxLights, 0, &dataPtr);

    var handle = mem.Pin();

    System.Buffer.MemoryCopy(handle.Pointer, dataPtr, (long)LarkLightShaderSize * (long)DefaultMaxLights, (long)LarkLightShaderSize * mem.Length);

    shareData.vk.UnmapMemory(shareData.Device, LightDataBuffer.Memory);

    handle.Dispose();
  }

  // public unsafe void UpdateGBufferDescriptorSets(int frameIndex) {

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

  //   var gbufferNormalImageInfo = new DescriptorImageInfo {
  //     Sampler = geometryPass.Normals.Span[frameIndex].Sampler,
  //     ImageView = geometryPass.Normals.Span[frameIndex].View,
  //     ImageLayout = ImageLayout.ShaderReadOnlyOptimal
  //   };

  //   var gbufferNormalWrite = new WriteDescriptorSet {
  //     SType = StructureType.WriteDescriptorSet,
  //     DstSet = SamplerSets.Span[frameIndex],
  //     DstBinding = 1,
  //     DstArrayElement = 0,
  //     DescriptorCount = 1,
  //     DescriptorType = DescriptorType.CombinedImageSampler,
  //     PImageInfo = &gbufferNormalImageInfo
  //   };

  //   shareData.vk.UpdateDescriptorSets(shareData.Device, 1, &gbufferNormalWrite, 0, null);

  //   var gbufferDepthImageInfo = new DescriptorImageInfo {
  //     Sampler = geometryPass.Depth.Span[frameIndex].Sampler,
  //     ImageView = geometryPass.Depth.Span[frameIndex].View,
  //     ImageLayout = ImageLayout.ShaderReadOnlyOptimal
  //   };

  //   var gbufferDepthWrite = new WriteDescriptorSet {
  //     SType = StructureType.WriteDescriptorSet,
  //     DstSet = SamplerSets.Span[frameIndex],
  //     DstBinding = 2,
  //     DstArrayElement = 0,
  //     DescriptorCount = 1,
  //     DescriptorType = DescriptorType.CombinedImageSampler,
  //     PImageInfo = &gbufferDepthImageInfo
  //   };

  //   shareData.vk.UpdateDescriptorSets(shareData.Device, 1, &gbufferDepthWrite, 0, null);
  // }

  // public unsafe void BindDescriptorSets(int frameIndex) {
  //   var handle = SamplerSets.Pin();

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
  // }

  public unsafe void CreateLightingRenderPass() {
    // This runs after the geometry render pass.
    // We take a gbuffer and fill a new buffer with lighting information.
    // This will use the lighting.vert,lighting.frag shaders.

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

    var subpass = new SubpassDescription {
      PipelineBindPoint = PipelineBindPoint.Graphics,
      ColorAttachmentCount = 1,
      PColorAttachments = &colorAttachmentRef
    };

    var attachments = new[] { colorAttachment };

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

      if (shareData.vk.CreateRenderPass(shareData.Device, &renderPassInfo, null, out Data.RenderPass) != Result.Success) {
        throw new Exception("failed to create render pass!");
      }
    }
  }

  private unsafe void CreateDescriptorPool() {
    var gbufferSize = (uint)(LarkVulkanData.MaxFramesInFlight * 3); // geo and normal and depth
    var imageSize = (uint)LarkVulkanData.MaxFramesInFlight * 2;
    var poolSizes = stackalloc DescriptorPoolSize[] {
      new DescriptorPoolSize {
        Type = DescriptorType.CombinedImageSampler,
        DescriptorCount = gbufferSize
      },
      new DescriptorPoolSize {
        Type = DescriptorType.StorageBuffer,
        DescriptorCount = imageSize
      }
    };

    var poolInfo = new DescriptorPoolCreateInfo {
      SType = StructureType.DescriptorPoolCreateInfo,
      PoolSizeCount = 2,
      PPoolSizes = poolSizes,
      MaxSets = imageSize + gbufferSize
    };

    if (shareData.vk.CreateDescriptorPool(shareData.Device, &poolInfo, null, out Data.DescriptorPool) != Result.Success) {
      throw new Exception("failed to create descriptor pool!");
    }
  }

  // private unsafe void CreateDescriptorSetLayouts() {
  //   logger.LogInformation("Creating GBuffer descriptor set layouts...");
  //   // GBuffer layout
  //   var gBufferLayoutBinding = new DescriptorSetLayoutBinding {
  //     Binding = 0,
  //     DescriptorCount = 1,
  //     DescriptorType = DescriptorType.CombinedImageSampler,
  //     PImmutableSamplers = null,
  //     StageFlags = ShaderStageFlags.FragmentBit
  //   };

  //   var gBufferNormalLayoutBinding = new DescriptorSetLayoutBinding {
  //     Binding = 1,
  //     DescriptorCount = 1,
  //     DescriptorType = DescriptorType.CombinedImageSampler,
  //     PImmutableSamplers = null,
  //     StageFlags = ShaderStageFlags.FragmentBit
  //   };

  //   var gBufferDepthLayoutBinding = new DescriptorSetLayoutBinding {
  //     Binding = 2,
  //     DescriptorCount = 1,
  //     DescriptorType = DescriptorType.CombinedImageSampler,
  //     PImmutableSamplers = null,
  //     StageFlags = ShaderStageFlags.FragmentBit
  //   };

  //   var (gBufferBindingMem, gBufferBindingSize) = RegisterMemory(new[] { gBufferLayoutBinding, gBufferNormalLayoutBinding, gBufferDepthLayoutBinding });

  //   var gBufferLayoutInfo = new DescriptorSetLayoutCreateInfo {
  //     SType = StructureType.DescriptorSetLayoutCreateInfo,
  //     BindingCount = gBufferBindingSize,
  //     PBindings = (DescriptorSetLayoutBinding*)gBufferBindingMem.Pointer
  //   };

  //   if (shareData.vk.CreateDescriptorSetLayout(shareData.Device, &gBufferLayoutInfo, null, out DescriptorSetLayout gBufferLayout) != Result.Success) {
  //     throw new Exception("failed to create descriptor set layout!");
  //   }

  //   Data.DescriptorSetLayouts.Add(Layouts.GBuffers, gBufferLayout);


  //   // var gBufferNormalLayoutInfo = new DescriptorSetLayoutCreateInfo {
  //   //   SType = StructureType.DescriptorSetLayoutCreateInfo,
  //   //   BindingCount = 1,
  //   //   PBindings = &gBufferNormalLayoutBinding
  //   // };

  //   // if (shareData.vk.CreateDescriptorSetLayout(shareData.Device, &gBufferNormalLayoutInfo, null, out DescriptorSetLayout gBufferNormalLayout) != Result.Success) {
  //   //   throw new Exception("failed to create descriptor set layout!");
  //   // }

  //   // Data.DescriptorSetLayouts.Add(Layouts.GBufferNormals, gBufferNormalLayout);

  //   logger.LogInformation("Created light descriptor set layouts...");

  //   // Lightdata layout
  //   var lightLayoutBinding = new DescriptorSetLayoutBinding {
  //     Binding = 0,
  //     DescriptorCount = 1,
  //     DescriptorType = DescriptorType.StorageBuffer,
  //     PImmutableSamplers = null,
  //     StageFlags = ShaderStageFlags.FragmentBit
  //   };

  //   var lightLayoutInfo = new DescriptorSetLayoutCreateInfo {
  //     SType = StructureType.DescriptorSetLayoutCreateInfo,
  //     BindingCount = 1,
  //     PBindings = &lightLayoutBinding
  //   };

  //   if (shareData.vk.CreateDescriptorSetLayout(shareData.Device, &lightLayoutInfo, null, out DescriptorSetLayout lightLayout) != Result.Success) {
  //     throw new Exception("failed to create descriptor set layout!");
  //   }

  //   Data.DescriptorSetLayouts.Add(Layouts.Lights, lightLayout);

  //   logger.LogInformation("Created descriptor set layouts.");
  // }

  private unsafe void CreateGraphicsPipeline() {
    var vertShaderInfo = shaderBuilder.LoadShader("lighting.vert");
    var fragShaderInfo = shaderBuilder.LoadShader("lighting.frag");

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

    // Don't think we would have the usual vertex bindings here given we just have a quad
    var layouts = Data.PipelineSets.Values.Select(x => x.Layout).ToArray();
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

    var cameraConstants = new PushConstantRange {
      StageFlags = ShaderStageFlags.FragmentBit,
      Offset = 0,
      Size = (uint)Marshal.SizeOf<LarkCameraConstants>()
    };

    var pushConstants = new[] { cameraConstants };
    var (pushConstantsMem, pushConstantsSize) = RegisterMemory(pushConstants);

    var pipelineLayoutInfo = new PipelineLayoutCreateInfo {
      SType = StructureType.PipelineLayoutCreateInfo,
      SetLayoutCount = setLayoutsSize,
      PSetLayouts = (DescriptorSetLayout*)setLayoutsMem.Pointer,
      PushConstantRangeCount = pushConstantsSize,
      PPushConstantRanges = (PushConstantRange*)pushConstantsMem.Pointer
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

    var depthStencil = new PipelineDepthStencilStateCreateInfo {
      SType = StructureType.PipelineDepthStencilStateCreateInfo,
      DepthTestEnable = Vk.False,
      DepthWriteEnable = Vk.False,
      DepthCompareOp = CompareOp.Less,
      DepthBoundsTestEnable = Vk.False,
      StencilTestEnable = Vk.False
    };

    var pipelineInfo = new GraphicsPipelineCreateInfo {
      SType = StructureType.GraphicsPipelineCreateInfo,
      StageCount = 2,
      PStages = shaderStages,
      PViewportState = &viewportState,
      PRasterizationState = &rasterizer,
      PMultisampleState = &multisampling,
      PDepthStencilState = &depthStencil,
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

    logger.LogInformation("Created Lighting pipeline.");
  }
}