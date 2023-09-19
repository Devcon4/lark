using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace Lark.Engine.Pipeline;

public struct UniformBufferObject {
  public Matrix4X4<float> Model;
  public Matrix4X4<float> View;
  public Matrix4X4<float> Projection;
  public Matrix4X4<float> Normal;
}

public class DescriptorSetSegment(LarkVulkanData data, ILogger<DescriptorSetSegment> logger) {


  public unsafe void CreateDescriptorPool() {
    var poolSizes = new DescriptorPoolSize[3];

    poolSizes[0] = new DescriptorPoolSize {
      Type = DescriptorType.UniformBuffer,
      DescriptorCount = LarkVulkanData.MaxFramesInFlight
    };

    poolSizes[1] = new DescriptorPoolSize {
      Type = DescriptorType.CombinedImageSampler,
      DescriptorCount = LarkVulkanData.MaxFramesInFlight
    };

    poolSizes[2] = new DescriptorPoolSize {
      Type = DescriptorType.CombinedImageSampler,
      DescriptorCount = LarkVulkanData.MaxFramesInFlight
    };

    var poolInfo = new DescriptorPoolCreateInfo {
      SType = StructureType.DescriptorPoolCreateInfo,
      PoolSizeCount = (uint)poolSizes.Length,
      PPoolSizes = (DescriptorPoolSize*)poolSizes.AsMemory().Pin().Pointer,
      MaxSets = LarkVulkanData.MaxFramesInFlight
    };

    if (data.vk.CreateDescriptorPool(data.Device, &poolInfo, null, out data.DescriptorPool) != Result.Success) {
      throw new Exception("failed to create descriptor pool!");
    }

    logger.LogInformation("Created descriptor pool.");
  }

  public unsafe void CreateDescriptorSets() {
    logger.LogInformation("Creating descriptor sets...");

    var layouts = new DescriptorSetLayout[LarkVulkanData.MaxFramesInFlight];
    for (var i = 0; i < LarkVulkanData.MaxFramesInFlight; i++) {
      layouts[i] = data.DescriptorSetLayout;
    }

    var allocInfo = new DescriptorSetAllocateInfo {
      SType = StructureType.DescriptorSetAllocateInfo,
      DescriptorPool = data.DescriptorPool,
      DescriptorSetCount = LarkVulkanData.MaxFramesInFlight,
      PSetLayouts = (DescriptorSetLayout*)layouts.AsMemory().Pin().Pointer
    };

    data.DescriptorSets = new DescriptorSet[LarkVulkanData.MaxFramesInFlight];
    if (data.vk.AllocateDescriptorSets(data.Device, &allocInfo, data.DescriptorSets) != Result.Success) {
      throw new Exception("failed to allocate descriptor sets!");
    }

    for (var i = 0; i < LarkVulkanData.MaxFramesInFlight; i++) {
      logger.LogInformation("Created descriptor set {item}.", i);
      var bufferInfo = new DescriptorBufferInfo {
        Buffer = data.UniformBuffers[i],
        Offset = 0,
        Range = (ulong)sizeof(UniformBufferObject)
      };

      var imageInfo = new DescriptorImageInfo {
        Sampler = data.TextureSampler,
        ImageView = data.TextureImageView,
        ImageLayout = ImageLayout.ShaderReadOnlyOptimal
      };

      // var normalImageInfo = new DescriptorImageInfo {
      //   Sampler = data.TextureSampler,
      //   ImageView = data.NormalImageView,
      //   ImageLayout = ImageLayout.ShaderReadOnlyOptimal
      // };

      logger.LogInformation("DescriptorWrites set {item}.", i);

      var descriptorWrites = new WriteDescriptorSet[2];

      descriptorWrites[0] = new WriteDescriptorSet {
        SType = StructureType.WriteDescriptorSet,
        DstSet = data.DescriptorSets[i],
        DstBinding = 0,
        DstArrayElement = 0,
        DescriptorType = DescriptorType.UniformBuffer,
        DescriptorCount = 1,
        PBufferInfo = &bufferInfo
      };

      descriptorWrites[1] = new WriteDescriptorSet {
        SType = StructureType.WriteDescriptorSet,
        DstSet = data.DescriptorSets[i],
        DstBinding = 1,
        DstArrayElement = 0,
        DescriptorType = DescriptorType.CombinedImageSampler,
        DescriptorCount = 1,
        PImageInfo = &imageInfo
      };

      // descriptorWrites[2] = new WriteDescriptorSet {
      //   SType = StructureType.WriteDescriptorSet,
      //   DstSet = data.DescriptorSets[i],
      //   DstBinding = 2,
      //   DstArrayElement = 0,
      //   DescriptorType = DescriptorType.CombinedImageSampler,
      //   DescriptorCount = 1,
      //   PImageInfo = &normalImageInfo
      // };

      logger.LogInformation("Writing descriptor set {item}.", i);

      fixed (WriteDescriptorSet* descriptorWritesPtr = descriptorWrites) {
        data.vk.UpdateDescriptorSets(data.Device, (uint)descriptorWrites.Length, descriptorWritesPtr, 0, null);
      }
    }

    logger.LogInformation("Created descriptor sets.");
  }

  public unsafe void CreateDescriptorSetLayout() {
    var uboLayoutBinding = new DescriptorSetLayoutBinding {
      Binding = 0,
      DescriptorCount = 1,
      DescriptorType = DescriptorType.UniformBuffer,
      PImmutableSamplers = null,
      StageFlags = ShaderStageFlags.VertexBit
    };

    var samplerLayoutBinding = new DescriptorSetLayoutBinding {
      Binding = 1,
      DescriptorCount = 1,
      DescriptorType = DescriptorType.CombinedImageSampler,
      PImmutableSamplers = null,
      StageFlags = ShaderStageFlags.FragmentBit
    };

    var normalSamplerLayoutBinding = new DescriptorSetLayoutBinding {
      Binding = 2,
      DescriptorCount = 1,
      DescriptorType = DescriptorType.CombinedImageSampler,
      PImmutableSamplers = null,
      StageFlags = ShaderStageFlags.FragmentBit
    };

    var bindings = new[] { uboLayoutBinding, samplerLayoutBinding }; //normalSamplerLayoutBinding 

    var layoutInfo = new DescriptorSetLayoutCreateInfo {
      SType = StructureType.DescriptorSetLayoutCreateInfo,
      BindingCount = (uint)bindings.Length,
      PBindings = (DescriptorSetLayoutBinding*)bindings.AsMemory().Pin().Pointer
    };

    if (data.vk.CreateDescriptorSetLayout(data.Device, &layoutInfo, null, out data.DescriptorSetLayout) != Result.Success) {
      throw new Exception("failed to create descriptor set layout!");
    }

    logger.LogInformation("Created descriptor set layout.");
  }
}