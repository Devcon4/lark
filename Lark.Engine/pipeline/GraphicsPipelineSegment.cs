using Microsoft.Extensions.Logging;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace Lark.Engine.pipeline;

public class GraphicsPipelineSegment(LarkVulkanData data, ShaderBuilder shaderBuilder, ILogger<GraphicsPipelineSegment> logger) {
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

    var bindingDescription = Vertex.GetBindingDescription();
    var attributeDescriptions = Vertex.GetAttributeDescriptions();
    var d = data.Layouts;

    var setLayouts = new[] { data.Layouts.matricies, data.Layouts.textures };

    fixed (VertexInputAttributeDescription* attributeDescriptionsPtr = attributeDescriptions)
    fixed (DescriptorSetLayout* setLayoutsPtr = setLayouts) {

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
        Width = data.SwapchainExtent.Width,
        Height = data.SwapchainExtent.Height,
        MinDepth = 0.0f,
        MaxDepth = 1.0f
      };

      var scissor = new Rect2D { Offset = default, Extent = data.SwapchainExtent };

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
        FrontFace = FrontFace.CounterClockwise,
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

      var dynamicStates = stackalloc[] {
        DynamicState.Viewport,
        DynamicState.LineWidth
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
        SetLayoutCount = (uint)setLayouts.Length,
        PushConstantRangeCount = 1,
        PPushConstantRanges = &pushConstantRange,
        PSetLayouts = setLayoutsPtr
      };

      if (data.vk.CreatePipelineLayout(data.Device, &pipelineLayoutInfo, null, out data.PipelineLayout) != Result.Success) {
        throw new Exception("failed to create pipeline layout!");
      }

      var pipeline = data.PipelineLayout;

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
        Layout = data.PipelineLayout,
        RenderPass = data.RenderPass,
        Subpass = 0,
        BasePipelineHandle = default
      };

      if (data.vk.CreateGraphicsPipelines(data.Device, default, 1, &pipelineInfo, null, out data.GraphicsPipeline) != Result.Success) {
        throw new Exception("failed to create graphics pipeline!");
      }
    }

    data.vk.DestroyShaderModule(data.Device, fragShaderModule, null);
    data.vk.DestroyShaderModule(data.Device, vertShaderModule, null);

    logger.LogInformation("Created graphics pipeline.");
  }

  private unsafe ShaderModule CreateShaderModule(byte[] code) {
    var createInfo = new ShaderModuleCreateInfo {
      SType = StructureType.ShaderModuleCreateInfo,
      CodeSize = (nuint)code.Length
    };
    fixed (byte* codePtr = code) {
      createInfo.PCode = (uint*)codePtr;
    }

    var shaderModule = new ShaderModule();
    if (data.vk.CreateShaderModule(data.Device, &createInfo, null, &shaderModule) != Result.Success) {
      throw new Exception("failed to create shader module!");
    }

    return shaderModule;
  }
}