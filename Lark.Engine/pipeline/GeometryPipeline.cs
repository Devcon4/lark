using System.Numerics;
using System.Runtime.InteropServices;
using Lark.Engine.model;
using Microsoft.Extensions.Logging;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Lark.Engine.pipeline;

public class GeometryPassData() {
  public Memory<LarkImage> Images = new(new LarkImage[LarkVulkanData.MaxFramesInFlight]);
  public Memory<LarkImage> Normals = new(new LarkImage[LarkVulkanData.MaxFramesInFlight]);
  public Memory<LarkImage> Depth = new(new LarkImage[LarkVulkanData.MaxFramesInFlight]);
}

public class GeometryPipeline(ILogger<GeometryPipeline> logger, GeometryPassData geometryPass, LarkVulkanData shareData, ImageUtils imageUtils, ShaderBuilder shaderBuilder) : LarkPipeline(shareData) {
  public Dictionary<string, DescriptorSetLayout> DescriptorSetLayouts = [];

  public override void Start() {
    logger.LogInformation("Starting Geometry pipeline...");

    // Geo is kinda special, descriptorSets are tracked per model. Because of this we don't use PipelineSets.
    // We could look at refactoring this to use PipelineSets in the future.

    CreateGBufferImages();
    CreateDescriptorSetLayouts();
    CreateRenderPass();
    CreateClearValues();
    CreateFramebuffers();
    CreateGraphicsPipeline();
  }

  public readonly struct Layouts() {
    public static readonly string Matricies = "Matrices";
    public static readonly string Textures = "Textures";
  }

  public override unsafe void Draw(uint index) {
    // Transition the images to the correct layout
    TransitionGBufferImages((int)index, ImageLayout.ColorAttachmentOptimal);
    imageUtils.TransitionImageLayout(ref geometryPass.Depth.Span[(int)index], ImageLayout.DepthStencilAttachmentOptimal, ImageAspectFlags.DepthBit);

    foreach (var (key, instance) in shareData.instances) {
      var model = shareData.models[instance.ModelId];
      DrawMesh(instance.Transform, model, index);
    }

    TransitionGBufferImages((int)index, ImageLayout.ShaderReadOnlyOptimal);
    imageUtils.TransitionImageLayout(ref geometryPass.Depth.Span[(int)index], ImageLayout.ShaderReadOnlyOptimal, ImageAspectFlags.DepthBit);
  }

  private void TransitionGBufferImages(int index, ImageLayout newLayout) {
    imageUtils.TransitionImageLayout(ref geometryPass.Images.Span[index], newLayout);
    imageUtils.TransitionImageLayout(ref geometryPass.Normals.Span[index], newLayout);
  }

  public unsafe override void Cleanup() {
    foreach (var layout in DescriptorSetLayouts.Values) {
      shareData.vk.DestroyDescriptorSetLayout(shareData.Device, layout, null);
    }

    foreach (var image in geometryPass.Images.Span) {
      image.Dispose(shareData);
    }

    foreach (var image in geometryPass.Normals.Span) {
      image.Dispose(shareData);
    }

    foreach (var image in geometryPass.Depth.Span) {
      image.Dispose(shareData);
    }

    foreach (var framebuffer in Data.Framebuffers) {
      shareData.vk.DestroyFramebuffer(shareData.Device, framebuffer, null);
    }

    shareData.vk.DestroyPipeline(shareData.Device, Data.Pipeline, null);
    shareData.vk.DestroyRenderPass(shareData.Device, Data.RenderPass, null);
    shareData.vk.DestroyPipelineLayout(shareData.Device, Data.PipelineLayout, null);


    base.Cleanup();
  }


  private unsafe void DrawNode(LarkNode node, LarkTransform parentTransform, LarkModel model, uint index) {

    var absoluteTransform = node.Transform * parentTransform;

    foreach (var child in node.Children) {
      DrawNode(child, absoluteTransform, model, index);
    }

    // PushConsts cant be generic types so we use a system matrix.
    var absoluteMatrix = absoluteTransform.ToMatrix().ToSystem();

    // Setup the push constants.
    shareData.vk.CmdPushConstants(
      shareData.CommandBuffers[index],
      Data.PipelineLayout,
      ShaderStageFlags.VertexBit,
      0,
      (uint)Marshal.SizeOf<Matrix4x4>(),
      &absoluteMatrix
    );

    if (node.Primitives == null || node.Primitives.Length == 0) return;

    // TODO: pass the matrix in as a push constant.
    foreach (var primitive in node.Primitives) {
      // if (primitive.IndexCount == 0) continue;
      var texIndex = model.Materials.Span[primitive.MaterialIndex].BaseColorTextureIndex ?? 0;
      var texture = model.Textures.Span[texIndex];
      shareData.vk.CmdBindDescriptorSets(
        shareData.CommandBuffers[index],
        PipelineBindPoint.Graphics,
        Data.PipelineLayout,
        1,
        1,
        model.Images.Span[texture.TextureIndex].DescriptorSets[index],
        0,
        null
      );

      shareData.vk.CmdDrawIndexed(shareData.CommandBuffers[index], (uint)primitive.IndexCount, 1, (uint)primitive.FirstIndex, 0, 0);
    }
  }


  public unsafe void DrawMesh(LarkTransform transform, LarkModel model, uint index) {
    var offsets = new ulong[] { 0 };
    // Bind the descriptor set for the matrix.
    shareData.vk.CmdBindDescriptorSets(
      shareData.CommandBuffers[index],
      PipelineBindPoint.Graphics,
      Data.PipelineLayout,
      0,
      1,
      model.MatrixDescriptorSet.Span[(int)index],
      0,
      null
    );

    fixed (Buffer* verticesPtr = &model.Vertices.Buffer) {
      shareData.vk.CmdBindVertexBuffers(shareData.CommandBuffers[index], 0, 1, verticesPtr, offsets);
      shareData.vk.CmdBindIndexBuffer(shareData.CommandBuffers[index], model.Indices.Buffer, 0, IndexType.Uint16);

      for (var i = 0; i < model.Nodes.Length; i++) {
        DrawNode(model.Nodes.Span[i], model.Transform * transform, model, index);
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
      },
      new() {
        Color = new ClearColorValue { Float32_0 = 0, Float32_1 = 0, Float32_2 = 0, Float32_3 = 0 }
      }
    ];
  }

  // public unsafe void CreateDepthImage() {
  //   var depthFormat = imageUtils.FindDepthFormat();

  //   imageUtils.CreateImage(shareData.SwapchainExtent.Width, shareData.SwapchainExtent.Height, depthFormat, ImageTiling.Optimal, ImageUsageFlags.DepthStencilAttachmentBit, MemoryPropertyFlags.DeviceLocalBit, ref DepthImage.Image, ref DepthImage.Memory);
  //   DepthImage.View = imageUtils.CreateImageView(DepthImage.Image, depthFormat, ImageAspectFlags.DepthBit);
  //   // imageUtils.TransitionImageLayout(DepthImage.Image, ImageLayout.Undefined, ImageLayout.DepthStencilAttachmentOptimal);
  // }

  private unsafe void CreateGBufferImages() {
    CreateGbufferPart(ref geometryPass.Images);
    CreateGbufferPart(ref geometryPass.Normals);
    CreateGbufferPart(ref geometryPass.Depth, ImageAspectFlags.DepthBit, imageUtils.FindDepthFormat(), ImageUsageFlags.InputAttachmentBit | ImageUsageFlags.SampledBit | ImageUsageFlags.DepthStencilAttachmentBit);
  }

  private unsafe void CreateGbufferPart(ref Memory<LarkImage> images, ImageAspectFlags aspect = ImageAspectFlags.ColorBit, Format format = Format.B8G8R8A8Unorm, ImageUsageFlags usage = ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.InputAttachmentBit | ImageUsageFlags.SampledBit) {
    for (int i = 0; i < LarkVulkanData.MaxFramesInFlight; i++) {
      imageUtils.CreateImage(shareData.SwapchainExtent.Width, shareData.SwapchainExtent.Height, format, ImageTiling.Optimal, usage, MemoryPropertyFlags.DeviceLocalBit, ref images.Span[i].Image, ref images.Span[i].Memory);
      // create image view and sampler
      images.Span[i].View = imageUtils.CreateImageView(images.Span[i].Image, format, aspect);
      CreateGBufferSampler(ref images.Span[i].Sampler);
      // imageUtils.TransitionImageLayout(images.Span[i].Image, ImageLayout.Undefined, ImageLayout.ColorAttachmentOptimal);
    }

  }

  private unsafe void CreateGBufferSampler(ref Sampler GBufferSampler) {
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

    if (shareData.vk.CreateSampler(shareData.Device, &samplerInfo, null, out GBufferSampler) != Result.Success) {
      throw new Exception("failed to create texture sampler!");
    }
  }

  public unsafe void CreateFramebuffers() {
    // This will create a framebuffer with multiple color attachments.
    // This will be used to store the geometry information of the scene.
    Data.Framebuffers = new Framebuffer[geometryPass.Images.Length];

    for (int i = 0; i < geometryPass.Images.Length; i++) {
      var attachments = new[] { geometryPass.Images.Span[i].View, geometryPass.Depth.Span[i].View, geometryPass.Normals.Span[i].View };

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

  public unsafe void CreateDescriptorSetLayouts() {
    var uboLayoutBinding = new DescriptorSetLayoutBinding {
      Binding = 0,
      DescriptorCount = 1,
      DescriptorType = DescriptorType.UniformBuffer,
      PImmutableSamplers = null,
      StageFlags = ShaderStageFlags.VertexBit
    };

    var uboLayoutInfo = new DescriptorSetLayoutCreateInfo {
      SType = StructureType.DescriptorSetLayoutCreateInfo,
      BindingCount = 1,
      PBindings = &uboLayoutBinding
    };

    if (shareData.vk.CreateDescriptorSetLayout(shareData.Device, &uboLayoutInfo, null, out DescriptorSetLayout matricesLayout) != Result.Success) {
      throw new Exception("failed to create descriptor set layout!");
    }

    DescriptorSetLayouts.Add(Layouts.Matricies, matricesLayout);

    var textureLayoutBinding = new DescriptorSetLayoutBinding {
      Binding = 0,
      DescriptorCount = 1,
      DescriptorType = DescriptorType.CombinedImageSampler,
      PImmutableSamplers = null,
      StageFlags = ShaderStageFlags.FragmentBit
    };

    var textureLayoutInfo = new DescriptorSetLayoutCreateInfo {
      SType = StructureType.DescriptorSetLayoutCreateInfo,
      BindingCount = 1,
      PBindings = &textureLayoutBinding
    };

    if (shareData.vk.CreateDescriptorSetLayout(shareData.Device, &textureLayoutInfo, null, out DescriptorSetLayout textureLayout) != Result.Success) {
      throw new Exception("failed to create descriptor set layout!");
    }

    DescriptorSetLayouts.Add(Layouts.Textures, textureLayout);

    logger.LogInformation("Created descriptor set layouts.");
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
      FinalLayout = ImageLayout.ShaderReadOnlyOptimal
    };

    var normalAttachment = new AttachmentDescription {
      Format = shareData.SwapchainImageFormat,
      Samples = SampleCountFlags.Count1Bit,
      LoadOp = AttachmentLoadOp.Clear,
      StoreOp = AttachmentStoreOp.Store,
      StencilLoadOp = AttachmentLoadOp.DontCare,
      StencilStoreOp = AttachmentStoreOp.DontCare,
      InitialLayout = ImageLayout.Undefined,
      FinalLayout = ImageLayout.ShaderReadOnlyOptimal
    };

    var depthAttachment = new AttachmentDescription {
      Format = imageUtils.FindDepthFormat(),
      Samples = SampleCountFlags.Count1Bit,
      LoadOp = AttachmentLoadOp.Clear,
      StoreOp = AttachmentStoreOp.DontCare,
      StencilLoadOp = AttachmentLoadOp.DontCare,
      StencilStoreOp = AttachmentStoreOp.DontCare,
      InitialLayout = ImageLayout.Undefined,
      FinalLayout = ImageLayout.ShaderReadOnlyOptimal
    };

    var colorAttachmentRef = new AttachmentReference {
      Attachment = 0,
      Layout = ImageLayout.ColorAttachmentOptimal
    };

    var normalAttachmentRef = new AttachmentReference {
      Attachment = 2,
      Layout = ImageLayout.ColorAttachmentOptimal
    };

    var colorAttachmentRefs = new[] { colorAttachmentRef, normalAttachmentRef };
    var (colorAttachmentRefsMem, colorAttachmentRefsSize) = RegisterMemory(colorAttachmentRefs);

    var depthAttachmentRef = new AttachmentReference {
      Attachment = 1,
      Layout = ImageLayout.DepthStencilAttachmentOptimal
    };

    var subpass = new SubpassDescription {
      PipelineBindPoint = PipelineBindPoint.Graphics,
      ColorAttachmentCount = colorAttachmentRefsSize,
      PColorAttachments = (AttachmentReference*)colorAttachmentRefsMem.Pointer,
      PDepthStencilAttachment = &depthAttachmentRef
    };

    var attachments = new[] { colorAttachment, depthAttachment, normalAttachment };

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

  public unsafe void CreateGraphicsPipeline() {
    var vertShaderInfo = shaderBuilder.LoadShader("mesh.vert");
    var fragShaderInfo = shaderBuilder.LoadShader("mesh.frag");

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

    var bindingDescription = LarkVertex.GetBindingDescription();
    var attributeDescriptions = LarkVertex.GetAttributeDescriptions();
    var (setLayoutsMem, setLayoutsSize) = RegisterMemory(DescriptorSetLayouts.Values.ToArray());

    fixed (VertexInputAttributeDescription* attributeDescriptionsPtr = attributeDescriptions) {

      var vertexInputInfo = new PipelineVertexInputStateCreateInfo {
        SType = StructureType.PipelineVertexInputStateCreateInfo,
        VertexBindingDescriptionCount = 1,
        PVertexBindingDescriptions = &bindingDescription,
        VertexAttributeDescriptionCount = (uint)attributeDescriptions.Length,
        PVertexAttributeDescriptions = attributeDescriptionsPtr
      };

      var inputAssembly = new PipelineInputAssemblyStateCreateInfo {
        SType = StructureType.PipelineInputAssemblyStateCreateInfo,
        Topology = PrimitiveTopology.TriangleList,
        PrimitiveRestartEnable = Vk.False
      };

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
        LineWidth = 1f,
        CullMode = CullModeFlags.BackBit,
        FrontFace = FrontFace.Clockwise,
        DepthBiasEnable = Vk.False
      };

      var multisampling = new PipelineMultisampleStateCreateInfo {
        SType = StructureType.PipelineMultisampleStateCreateInfo,
        SampleShadingEnable = Vk.False,
        RasterizationSamples = SampleCountFlags.Count1Bit
      };

      var depthStencil = new PipelineDepthStencilStateCreateInfo {
        SType = StructureType.PipelineDepthStencilStateCreateInfo,
        DepthTestEnable = true,
        DepthWriteEnable = true,
        DepthCompareOp = CompareOp.Less,
        DepthBoundsTestEnable = false,
        StencilTestEnable = false
      };

      var colorBlendAttachment = new PipelineColorBlendAttachmentState {
        ColorWriteMask = ColorComponentFlags.RBit |
                           ColorComponentFlags.GBit |
                           ColorComponentFlags.BBit |
                           ColorComponentFlags.ABit,
        BlendEnable = Vk.False
      };

      var normalBlendAttachment = new PipelineColorBlendAttachmentState {
        ColorWriteMask = ColorComponentFlags.RBit |
                           ColorComponentFlags.GBit |
                           ColorComponentFlags.BBit |
                           ColorComponentFlags.ABit,
        BlendEnable = Vk.False
      };

      var attachments = new[] { colorBlendAttachment, normalBlendAttachment };
      var (attachmentsMem, attachmentsSize) = RegisterMemory(attachments);

      var colorBlending = new PipelineColorBlendStateCreateInfo {
        SType = StructureType.PipelineColorBlendStateCreateInfo,
        LogicOpEnable = Vk.False,
        LogicOp = LogicOp.Copy,
        AttachmentCount = attachmentsSize,
        PAttachments = (PipelineColorBlendAttachmentState*)attachmentsMem.Pointer
      };

      colorBlending.BlendConstants[0] = 0.0f;
      colorBlending.BlendConstants[1] = 0.0f;
      colorBlending.BlendConstants[2] = 0.0f;
      colorBlending.BlendConstants[3] = 0.0f;

      var dynamicStates = stackalloc[] {
        DynamicState.Viewport,
        DynamicState.LineWidth,
      };

      var dynamicState = new PipelineDynamicStateCreateInfo {
        SType = StructureType.PipelineDynamicStateCreateInfo,
        DynamicStateCount = 2,
        PDynamicStates = dynamicStates
      };

      //TODO: setup push constants.
      var pushConstantRange = new PushConstantRange {
        StageFlags = ShaderStageFlags.VertexBit,
        Size = (uint)sizeof(Matrix4X4<float>),
        Offset = 0
      };

      var pipelineLayoutInfo = new PipelineLayoutCreateInfo {
        SType = StructureType.PipelineLayoutCreateInfo,
        SetLayoutCount = setLayoutsSize,
        PushConstantRangeCount = 1,
        PPushConstantRanges = &pushConstantRange,
        PSetLayouts = (DescriptorSetLayout*)setLayoutsMem.Pointer
      };

      if (shareData.vk.CreatePipelineLayout(shareData.Device, &pipelineLayoutInfo, null, out Data.PipelineLayout) != Result.Success) {
        throw new Exception("failed to create pipeline layout!");
      }

      var pipelineInfo = new GraphicsPipelineCreateInfo {
        SType = StructureType.GraphicsPipelineCreateInfo,
        StageCount = 2,
        PStages = shaderStages,
        PVertexInputState = &vertexInputInfo,
        PInputAssemblyState = &inputAssembly,
        PViewportState = &viewportState,
        PRasterizationState = &rasterizer,
        PMultisampleState = &multisampling,
        PDepthStencilState = &depthStencil,
        PColorBlendState = &colorBlending,
        PDynamicState = &dynamicState,
        Layout = Data.PipelineLayout,
        RenderPass = Data.RenderPass,
        Subpass = 0,
        BasePipelineHandle = default
      };

      if (shareData.vk.CreateGraphicsPipelines(shareData.Device, default, 1, &pipelineInfo, null, out Data.Pipeline) != Result.Success) {
        throw new Exception("failed to create graphics pipeline!");
      }
    }

    shareData.vk.DestroyShaderModule(shareData.Device, fragShaderModule, null);
    shareData.vk.DestroyShaderModule(shareData.Device, vertShaderModule, null);

    logger.LogInformation("Created graphics pipeline.");
  }
}