using System.Buffers;
using Lark.Engine.pipeline;
using Microsoft.Extensions.Logging;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using ImpromptuNinjas.UltralightSharp;

namespace Lark.Engine.Ultralight;


public class UIPassData() {
  public Memory<LarkImage> FinalImages = new(new LarkImage[LarkVulkanData.MaxFramesInFlight]);
  public Memory<LarkImage> BitmapImages = new(new LarkImage[LarkVulkanData.MaxFramesInFlight]);
}

public class UltralightPipeline(LarkVulkanData shareData, UIPassData uiPass, UltralightController controller, BufferUtils bufferUtils, ImageUtils imageUtils, ShaderBuilder shaderBuilder, ILogger<UltralightPipeline> logger) : LarkPipeline(shareData) {

  public readonly struct Layouts {
    public static readonly string Textures = "Textures";
  }

  public override void Start() {
    logger.LogInformation("Starting Ultralight pipeline...");
    CreateDescriptorPool();
    DeclarePipelineSets();
    CreateSetLayouts();

    CreateRenderPass();
    CreateUIImages();
    CreateFramebuffers();
    CreateSet(Layouts.Textures);

    CreateGraphicsPipeline();
    CreateClearValues();
  }

  public override void Update(uint index) {
    CopyBitmapToImage((int)index);
  }

  private void DeclarePipelineSets() {
    RegisterSet(Layouts.Textures, 0, [
      new LarkLayoutBindingInfo(DescriptorType.CombinedImageSampler, ShaderStageFlags.FragmentBit, 0)
    ]);
  }

  private unsafe void UppdateTextureSets(uint index) {
    var textureUpdateInfo = new DescriptorImageInfo {
      Sampler = uiPass.BitmapImages.Span[(int)index].Sampler,
      ImageView = uiPass.BitmapImages.Span[(int)index].View,
      ImageLayout = ImageLayout.ShaderReadOnlyOptimal
    };

    UpdateSet(Layouts.Textures, index, new WriteDescriptorSet {
      SType = StructureType.WriteDescriptorSet,
      DstBinding = 0,
      DescriptorCount = 1,
      DescriptorType = DescriptorType.CombinedImageSampler,
      PImageInfo = &textureUpdateInfo
    });
  }

  public unsafe void CreateFramebuffers() {
    Data.Framebuffers = new Framebuffer[LarkVulkanData.MaxFramesInFlight];

    for (int i = 0; i < LarkVulkanData.MaxFramesInFlight; i++) {
      var attachments = new[] { uiPass.FinalImages.Span[i].View, uiPass.BitmapImages.Span[i].View };

      var (attachmentMem, attachmentSize) = RegisterMemory(attachments);

      var framebufferInfo = new FramebufferCreateInfo {
        SType = StructureType.FramebufferCreateInfo,
        RenderPass = Data.RenderPass,
        AttachmentCount = attachmentSize,
        PAttachments = (ImageView*)attachmentMem.Pointer,
        Width = shareData.SwapchainExtent.Width,
        Height = shareData.SwapchainExtent.Height,
        Layers = 1
      };

      if (shareData.vk.CreateFramebuffer(shareData.Device, &framebufferInfo, null, out Data.Framebuffers[i]) != Result.Success) {
        throw new Exception("failed to create framebuffer!");
      }
    }

  }
  public unsafe override void Draw(uint index) {

    imageUtils.TransitionImageLayout(ref uiPass.BitmapImages.Span[(int)index], ImageLayout.ShaderReadOnlyOptimal);

    UppdateTextureSets(index);
    BindSet(Layouts.Textures, index);

    shareData.vk.CmdDraw(shareData.CommandBuffers[index], 6, 1, 0, 0);

  }

  public unsafe override void Cleanup() {

    foreach (var image in uiPass.FinalImages.Span) {
      shareData.vk.DestroySampler(shareData.Device, image.Sampler, null);
      shareData.vk.DestroyImageView(shareData.Device, image.View, null);
      shareData.vk.DestroyImage(shareData.Device, image.Image, null);
      shareData.vk.FreeMemory(shareData.Device, image.Memory, null);
    }

    foreach (var image in uiPass.BitmapImages.Span) {
      shareData.vk.DestroySampler(shareData.Device, image.Sampler, null);
      shareData.vk.DestroyImageView(shareData.Device, image.View, null);
      shareData.vk.DestroyImage(shareData.Device, image.Image, null);
      shareData.vk.FreeMemory(shareData.Device, image.Memory, null);
    }

    foreach (var set in Data.PipelineSets.Values) {
      shareData.vk.DestroyDescriptorSetLayout(shareData.Device, set.Layout, null);
    }

    foreach (var framebuffer in Data.Framebuffers) {
      shareData.vk.DestroyFramebuffer(shareData.Device, framebuffer, null);
    }
    shareData.vk.DestroyDescriptorPool(shareData.Device, Data.DescriptorPool, null);

    shareData.vk.DestroyPipeline(shareData.Device, Data.Pipeline, null);
    shareData.vk.DestroyRenderPass(shareData.Device, Data.RenderPass, null);
    shareData.vk.DestroyPipelineLayout(shareData.Device, Data.PipelineLayout, null);

    base.Cleanup();

  }

  private unsafe void CopyBitmapToImage(int index) {
    var bitmap = controller.GetBitmap();
    var size = bitmap->GetSize();
    var width = bitmap->GetWidth();
    var height = bitmap->GetHeight();

    // Format: Bgra8UNormSrgb
    var bitmapData = bitmap->LockPixels();

    LarkBuffer stagingBuffer = default;
    // Create buffer we will copy the bitmap data to
    bufferUtils.CreateBuffer(size, new BufferAllocInfo {
      Usage = BufferUsageFlags.TransferSrcBit,
      Properties = MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
      SharingMode = SharingMode.Exclusive
    }, ref stagingBuffer);

    // Map the buffer memory so we can copy the bitmap data to it
    void* imgData;
    shareData.vk.MapMemory(shareData.Device, stagingBuffer.Memory, 0, size, 0, &imgData);
    var srcSpan = new Span<byte>(bitmapData, (int)size);
    var dstSpan = new Span<byte>(imgData, (int)size);
    srcSpan.CopyTo(dstSpan);

    // var arr = srcSpan.ToArray();
    // if (arr is not null && srcSpan.Length > 0) {
    //   // dump bitmapData to image file for debugging
    //   // Use imagesharp to convert the byte array to a bitmap
    //   var info = new SkiaSharp.SKImageInfo(800, 600, SkiaSharp.SKColorType.Rgba8888);
    //   var bitImg = new SkiaSharp.SKBitmap(info);
    //   var mem = new Memory<byte>(arr);
    //   var memPin = mem.Pin();
    //   bitImg.SetPixels((nint)memPin.Pointer);
    //   var image = SkiaSharp.SKImage.FromBitmap(bitImg);
    //   using var stream = new FileStream("ultralight_debug.png", FileMode.Create, FileAccess.Write);
    //   var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
    //   data.SaveTo(stream);
    // }

    // transition the UIImage so we can copy
    imageUtils.TransitionImageLayout(uiPass.BitmapImages.Span[index].Image, ImageLayout.Undefined, ImageLayout.TransferDstOptimal);
    imageUtils.CopyBufferToImage(stagingBuffer.Buffer, uiPass.BitmapImages.Span[index].Image, width, height);
    imageUtils.TransitionImageLayout(uiPass.BitmapImages.Span[index].Image, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);

    stagingBuffer.Dispose(shareData);

    bitmap->UnlockPixels();
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

  private unsafe void CreateUIImages() {
    for (int i = 0; i < LarkVulkanData.MaxFramesInFlight; i++) {
      imageUtils.CreateImage(shareData.SwapchainExtent.Width, shareData.SwapchainExtent.Height, Format.B8G8R8A8Unorm, ImageTiling.Optimal, ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit | ImageUsageFlags.InputAttachmentBit | ImageUsageFlags.ColorAttachmentBit, MemoryPropertyFlags.DeviceLocalBit, ref uiPass.FinalImages.Span[i].Image, ref uiPass.FinalImages.Span[i].Memory);
      uiPass.FinalImages.Span[i].View = imageUtils.CreateImageView(uiPass.FinalImages.Span[i].Image, Format.B8G8R8A8Unorm, ImageAspectFlags.ColorBit);
      CreateUISampler(ref uiPass.FinalImages.Span[i].Sampler);

      imageUtils.CreateImage(shareData.SwapchainExtent.Width, shareData.SwapchainExtent.Height, Format.B8G8R8A8Unorm, ImageTiling.Optimal, ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit | ImageUsageFlags.InputAttachmentBit | ImageUsageFlags.ColorAttachmentBit, MemoryPropertyFlags.DeviceLocalBit, ref uiPass.BitmapImages.Span[i].Image, ref uiPass.BitmapImages.Span[i].Memory);
      uiPass.BitmapImages.Span[i].View = imageUtils.CreateImageView(uiPass.BitmapImages.Span[i].Image, Format.B8G8R8A8Unorm, ImageAspectFlags.ColorBit);
      imageUtils.CreateSampler(ref uiPass.BitmapImages.Span[i].Sampler);
    }
  }

  private unsafe void CreateUISampler(ref Sampler UISampler) {
    var samplerInfo = new SamplerCreateInfo {
      SType = StructureType.SamplerCreateInfo,
      MagFilter = Filter.Linear,
      MinFilter = Filter.Linear,
      AddressModeU = SamplerAddressMode.ClampToEdge,
      AddressModeV = SamplerAddressMode.ClampToEdge,
      AddressModeW = SamplerAddressMode.ClampToEdge,
      AnisotropyEnable = Vk.False,
      BorderColor = BorderColor.IntOpaqueBlack,
      UnnormalizedCoordinates = Vk.False,
      CompareEnable = Vk.False,
      CompareOp = CompareOp.Always,
      MipmapMode = SamplerMipmapMode.Linear
    };

    if (shareData.vk.CreateSampler(shareData.Device, &samplerInfo, null, out UISampler) != Result.Success) {
      throw new Exception("failed to create texture sampler!");
    }
  }

  private unsafe void CreateDescriptorPool() {
    var poolSizes = new DescriptorPoolSize[] {
      new() {
        Type = DescriptorType.CombinedImageSampler,
        DescriptorCount = LarkVulkanData.MaxFramesInFlight
      }
    };

    var (poolSizesMem, _) = RegisterMemory(poolSizes);

    var poolInfo = new DescriptorPoolCreateInfo {
      SType = StructureType.DescriptorPoolCreateInfo,
      PoolSizeCount = 1,
      PPoolSizes = (DescriptorPoolSize*)poolSizesMem.Pointer,
      MaxSets = LarkVulkanData.MaxFramesInFlight
    };

    if (shareData.vk.CreateDescriptorPool(shareData.Device, &poolInfo, null, out Data.DescriptorPool) != Result.Success) {
      throw new Exception("failed to create descriptor pool!");
    }
  }

  private unsafe void CreateRenderPass() {
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

    var bitmapAttachment = new AttachmentDescription {
      Format = Format.B8G8R8A8Unorm,
      Samples = SampleCountFlags.Count1Bit,
      LoadOp = AttachmentLoadOp.DontCare,
      StoreOp = AttachmentStoreOp.Store,
      StencilLoadOp = AttachmentLoadOp.DontCare,
      StencilStoreOp = AttachmentStoreOp.DontCare,
      InitialLayout = ImageLayout.Undefined,
      FinalLayout = ImageLayout.ShaderReadOnlyOptimal
    };

    var bitmapAttachmentRef = new AttachmentReference {
      Attachment = 1,
      Layout = ImageLayout.ShaderReadOnlyOptimal
    };

    var (inputAttachmentsMem, inputAttachmentsSize) = RegisterMemory(new[] { bitmapAttachmentRef });

    var subpass = new SubpassDescription {
      PipelineBindPoint = PipelineBindPoint.Graphics,
      ColorAttachmentCount = 1,
      PColorAttachments = &colorAttachmentRef,
      InputAttachmentCount = inputAttachmentsSize,
      PInputAttachments = (AttachmentReference*)inputAttachmentsMem.Pointer
    };

    var dependency = new SubpassDependency() {
      SrcSubpass = Vk.SubpassExternal,
      DstSubpass = 0,
      SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
      SrcAccessMask = 0,
      DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
      DstAccessMask = AccessFlags.ColorAttachmentWriteBit | AccessFlags.DepthStencilAttachmentWriteBit
    };

    var attachments = new[] { colorAttachment, bitmapAttachment };

    var (attachmentMem, attachmentSize) = RegisterMemory(attachments);
    MemoryHandles.Add(attachmentMem);

    var renderPassInfo = new RenderPassCreateInfo {
      SType = StructureType.RenderPassCreateInfo,
      AttachmentCount = attachmentSize,
      PAttachments = (AttachmentDescription*)attachmentMem.Pointer,
      SubpassCount = 1,
      PSubpasses = &subpass,
      DependencyCount = 1,
      PDependencies = &dependency,
    };

    if (shareData.vk.CreateRenderPass(shareData.Device, &renderPassInfo, null, out Data.RenderPass) != Result.Success) {
      throw new Exception("failed to create render pass!");
    }
  }

  private unsafe void CreateGraphicsPipeline() {
    // Create Ultralight pipeline

    var vertShaderInfo = shaderBuilder.LoadShader("ultralight.vert");
    var fragShaderInfo = shaderBuilder.LoadShader("ultralight.frag");

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

    var layouts = Data.PipelineSets.Values.Select(set => set.Layout).ToArray();
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