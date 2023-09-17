using Microsoft.Extensions.Logging;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Lark.Engine.Pipeline;

public class GraphicsPipelineSegment(LarkVulkanData data, ShaderBuilder shaderBuilder, ILogger<GraphicsPipelineSegment> logger) {
  public unsafe void CreateGraphicsPipeline() {
    var vertShaderInfo = shaderBuilder.LoadShader("triangle.vert");
    var fragShaderInfo = shaderBuilder.LoadShader("triangle.frag");

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

    var shaderStages = stackalloc PipelineShaderStageCreateInfo[2];
    shaderStages[0] = vertShaderStageInfo;
    shaderStages[1] = fragShaderStageInfo;

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

    var pipelineLayoutInfo = new PipelineLayoutCreateInfo {
      SType = StructureType.PipelineLayoutCreateInfo,
      SetLayoutCount = 0,
      PushConstantRangeCount = 0
    };

    fixed (PipelineLayout* pipelineLayout = &data.PipelineLayout) {
      if (data.vk.CreatePipelineLayout(data.Device, &pipelineLayoutInfo, null, pipelineLayout) != Result.Success) {
        throw new Exception("failed to create pipeline layout!");
      }
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
      PColorBlendState = &colorBlending,
      Layout = data.PipelineLayout,
      RenderPass = data.RenderPass,
      Subpass = 0,
      BasePipelineHandle = default
    };

    fixed (Silk.NET.Vulkan.Pipeline* graphicsPipeline = &data.GraphicsPipeline) {
      if (data.vk.CreateGraphicsPipelines
              (data.Device, default, 1, &pipelineInfo, null, graphicsPipeline) != Result.Success) {
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