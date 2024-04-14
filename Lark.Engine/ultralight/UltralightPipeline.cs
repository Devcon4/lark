using System.Buffers;
using Lark.Engine.pipeline;
using Microsoft.Extensions.Logging;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using ImpromptuNinjas.UltralightSharp;
using System.Runtime.InteropServices;
using Lark.Engine.Model;

namespace Lark.Engine.Ultralight;

public interface ILarkPipeline {
  void Start();
  void Draw(uint index);
  void Update(uint index);
  void Cleanup();

  PipelineData Data { get; set; }
}
public class PipelineData {
  public RenderPass RenderPass;
  public PipelineLayout PipelineLayout;
  public Pipeline Pipeline;

  public Framebuffer[] Framebuffers = [];
  public Dictionary<string, DescriptorSetLayout> DescriptorSetLayouts = [];
  public DescriptorPool DescriptorPool;
}

public abstract class LarkPipeline : ILarkPipeline {
  public PipelineData Data { get; set; } = new();
  public HashSet<MemoryHandle> MemoryHandles { get; set; } = [];
  public virtual void Start() { }
  public virtual void Draw(uint index) { }
  public virtual void Update(uint index) { }
  public virtual void Cleanup() {
    foreach (var handle in MemoryHandles) {
      handle.Dispose();
    }
  }

  protected (MemoryHandle, uint) RegisterMemory<T>(T[] data) {
    var mem = new Memory<T>(data);
    var memHandle = mem.Pin();
    MemoryHandles.Add(memHandle);
    return (memHandle, (uint)mem.Length);
  }
}

public class UltralightPipeline(UltralightController controller, BufferUtils bufferUtils, ImageUtils imageUtils, ShaderBuilder shaderBuilder, LarkVulkanData shareData, ILogger<UltralightPipeline> logger) : LarkPipeline {

  private Memory<LarkImage> UIImages = new(new LarkImage[LarkVulkanData.MaxFramesInFlight]);

  public override void Start() {
    logger.LogInformation("Starting Ultralight pipeline...");
    CreateDescriptorSetLayouts();
    CreateDescriptorPool();
    CreateRenderPass();
    CreateFramebuffers();
    CreateUIImages();
    CreateDescriptorSets();
    CreateGraphicsPipeline();
  }

  public override void Update(uint index) {
    CopyBitmapToImage((int)index);
  }

  public unsafe void CreateFramebuffers() {
    Data.Framebuffers = new Framebuffer[shareData.SwapchainImageViews.Length];

    for (int i = 0; i < shareData.SwapchainImageViews.Length; i++) {
      var attachments = new[] { shareData.SwapchainImageViews[i] };

      var (attachmentMem, _) = RegisterMemory(attachments);

      var framebufferInfo = new FramebufferCreateInfo {
        SType = StructureType.FramebufferCreateInfo,
        RenderPass = Data.RenderPass,
        AttachmentCount = 1,
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
    shareData.vk.CmdBindDescriptorSets(
      shareData.CommandBuffers[index],
      PipelineBindPoint.Graphics,
      Data.PipelineLayout,
      0,
      1,
      UIImages.Span[(int)index].DescriptorSets[0],
      0,
      null
    );
    shareData.vk.CmdDraw(shareData.CommandBuffers[index], 6, 1, 0, 0);

  }

  public unsafe override void Cleanup() {
    foreach (var image in UIImages.Span) {
      image.Dispose(shareData);
    }

    foreach (var layout in Data.DescriptorSetLayouts.Values) {
      shareData.vk.DestroyDescriptorSetLayout(shareData.Device, layout, null);
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
    imageUtils.TransitionImageLayout(UIImages.Span[index].Image, ImageLayout.Undefined, ImageLayout.TransferDstOptimal);
    imageUtils.CopyBufferToImage(stagingBuffer.Buffer, UIImages.Span[index].Image, width, height);
    imageUtils.TransitionImageLayout(UIImages.Span[index].Image, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);

    stagingBuffer.Dispose(shareData);

    bitmap->UnlockPixels();
  }

  private unsafe void CreateUIImages() {
    for (int i = 0; i < LarkVulkanData.MaxFramesInFlight; i++) {
      imageUtils.CreateImage(shareData.SwapchainExtent.Width, shareData.SwapchainExtent.Height, Format.B8G8R8A8Unorm, ImageTiling.Optimal, ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit, MemoryPropertyFlags.DeviceLocalBit, ref UIImages.Span[i].Image, ref UIImages.Span[i].Memory);
      // create image view and sampler
      UIImages.Span[i].View = imageUtils.CreateImageView(UIImages.Span[i].Image, Format.B8G8R8A8Unorm, ImageAspectFlags.ColorBit);
      CreateUISampler(ref UIImages.Span[i].Sampler);
    }
  }

  private unsafe void CreateDescriptorSets() {
    var layouts = Enumerable.Repeat(Data.DescriptorSetLayouts["Textures"], LarkVulkanData.MaxFramesInFlight).ToArray();
    var (layoutsMem, layoutsSize) = RegisterMemory(layouts);
    for (int j = 0; j < UIImages.Length; j++) {

      var allocInfo = new DescriptorSetAllocateInfo {
        SType = StructureType.DescriptorSetAllocateInfo,
        DescriptorPool = Data.DescriptorPool,
        DescriptorSetCount = 1,
        PSetLayouts = (DescriptorSetLayout*)layoutsMem.Pointer
      };

      var descriptorSets = new DescriptorSet[1];
      if (shareData.vk.AllocateDescriptorSets(shareData.Device, &allocInfo, descriptorSets) != Result.Success) {
        throw new Exception("failed to allocate descriptor sets!");
      }

      var imageInfo = new DescriptorImageInfo {
        Sampler = UIImages.Span[j].Sampler,
        ImageView = UIImages.Span[j].View,
        ImageLayout = ImageLayout.ShaderReadOnlyOptimal
      };

      var descriptorWrite = new WriteDescriptorSet {
        SType = StructureType.WriteDescriptorSet,
        DstSet = descriptorSets[0],
        DstBinding = 0,
        DstArrayElement = 0,
        DescriptorCount = 1,
        DescriptorType = DescriptorType.CombinedImageSampler,
        PImageInfo = &imageInfo
      };

      shareData.vk.UpdateDescriptorSets(shareData.Device, 1, &descriptorWrite, 0, null);

      UIImages.Span[j].DescriptorSets = descriptorSets;
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

  private unsafe void CreateDescriptorSetLayouts() {
    var textureLayoutBinding = new DescriptorSetLayoutBinding {
      Binding = 0,
      DescriptorType = DescriptorType.CombinedImageSampler,
      DescriptorCount = 1,
      StageFlags = ShaderStageFlags.FragmentBit,
      PImmutableSamplers = null
    };

    var textureLayoutInfo = new DescriptorSetLayoutCreateInfo {
      SType = StructureType.DescriptorSetLayoutCreateInfo,
      BindingCount = 1,
      PBindings = &textureLayoutBinding
    };

    if (shareData.vk.CreateDescriptorSetLayout(shareData.Device, &textureLayoutInfo, null, out DescriptorSetLayout textureLayout) != Result.Success) {
      throw new Exception("failed to create descriptor set layout!");
    }

    Data.DescriptorSetLayouts.Add("Textures", textureLayout);
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
      LoadOp = AttachmentLoadOp.Load,
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

    // Don't think we would have the usual vertex bindings here given we just have a quad
    var setLayouts = new[] { Data.DescriptorSetLayouts["Textures"] };
    var (setLayoutsMem, setLayoutsSize) = RegisterMemory(Data.DescriptorSetLayouts.Values.ToArray());

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

  private unsafe ShaderModule CreateShaderModule(byte[] code) {
    var (codeMem, codeSize) = RegisterMemory(code);

    var createInfo = new ShaderModuleCreateInfo {
      SType = StructureType.ShaderModuleCreateInfo,
      CodeSize = codeSize,
      PCode = (uint*)codeMem.Pointer
    };

    var shaderModule = new ShaderModule();
    if (shareData.vk.CreateShaderModule(shareData.Device, &createInfo, null, &shaderModule) != Result.Success) {
      throw new Exception("failed to create shader module!");
    }

    return shaderModule;
  }


}