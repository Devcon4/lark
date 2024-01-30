using Microsoft.Extensions.Logging;
using Silk.NET.Vulkan;

namespace Lark.Engine.pipeline;

// SamplerSegment.
// Creates samplers for textures.
public class SamplerSegment(LarkVulkanData data, ILogger<SamplerSegment> logger) {
  public unsafe void CreateTextureSampler() {
    // properties.
    PhysicalDeviceProperties properties;
    data.vk.GetPhysicalDeviceProperties(data.PhysicalDevice, &properties);

    var samplerInfo = new SamplerCreateInfo {
      SType = StructureType.SamplerCreateInfo,
      MagFilter = Filter.Linear,
      MinFilter = Filter.Linear,
      AddressModeU = SamplerAddressMode.Repeat,
      AddressModeV = SamplerAddressMode.Repeat,
      AddressModeW = SamplerAddressMode.Repeat,
      AnisotropyEnable = false,
      // MaxAnisotropy = properties.Limits.MaxSamplerAnisotropy,
      BorderColor = BorderColor.IntOpaqueBlack,
      UnnormalizedCoordinates = false,
      CompareEnable = false,
      CompareOp = CompareOp.Always,
      MipmapMode = SamplerMipmapMode.Linear,
    };

    fixed (Sampler* sampler = &data.TextureSampler) {
      if (data.vk.CreateSampler(data.Device, &samplerInfo, null, sampler) != Result.Success) {
        throw new Exception("failed to create texture sampler!");
      }
    }

    logger.LogInformation("Created texture sampler.");
  }

  // CreateNormalSampler.

  public unsafe void CreateNormalSampler() {
    PhysicalDeviceProperties properties;
    data.vk.GetPhysicalDeviceProperties(data.PhysicalDevice, &properties);

    var samplerInfo = new SamplerCreateInfo {
      SType = StructureType.SamplerCreateInfo,
      MagFilter = Filter.Linear,
      MinFilter = Filter.Linear,
      AddressModeU = SamplerAddressMode.Repeat,
      AddressModeV = SamplerAddressMode.Repeat,
      AddressModeW = SamplerAddressMode.Repeat,
      AnisotropyEnable = true,
      MaxAnisotropy = properties.Limits.MaxSamplerAnisotropy,
      BorderColor = BorderColor.IntOpaqueBlack,
      UnnormalizedCoordinates = false,
      CompareEnable = false,
      CompareOp = CompareOp.Always,
      MipmapMode = SamplerMipmapMode.Linear,
    };

    fixed (Sampler* sampler = &data.NormalSampler) {
      if (data.vk.CreateSampler(data.Device, &samplerInfo, null, sampler) != Result.Success) {
        throw new Exception("failed to create normal sampler!");
      }
    }

    logger.LogInformation("Created normal sampler.");
  }
}