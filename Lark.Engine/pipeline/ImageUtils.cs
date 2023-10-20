using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
using Image = Silk.NET.Vulkan.Image;

namespace Lark.Engine.Pipeline;

public struct RawImageInfo { }

public class ImageUtils(LarkVulkanData data, BufferUtils bufferUtils, CommandUtils commandUtils) {

  public unsafe void CreateTexture(ReadOnlyMemory<byte> memory, ref Image image, ref DeviceMemory imageMemory) {
    var rawImage = SixLabors.ImageSharp.Image.Load<Rgba32>(memory.Span);
    CreateImage(rawImage, ref image, ref imageMemory);
  }

  public unsafe void CreateTexture(string textureName, ref Image image, ref DeviceMemory imageMemory) {
    var fullPath = Path.Join(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"./resources/textures/{textureName}");

    if (!File.Exists(fullPath)) {
      throw new FileNotFoundException($"Texture {textureName} does not exist at {fullPath}");
    }

    using var img = SixLabors.ImageSharp.Image.Load<Rgba32>(fullPath);
    CreateImage(img, ref image, ref imageMemory);
  }

  public void UpdateTexture(ReadOnlySpan<byte> span, ref Image image) {
    var rawImage = SixLabors.ImageSharp.Image.Load<Rgba32>(span);
    UpdateImage(rawImage, ref image);
  }

  public unsafe void UpdateImage(Image<Rgba32> rawImage, ref Image image) {
    var imageByteSize = (ulong)(rawImage.Width * rawImage.Height * rawImage.PixelType.BitsPerPixel / 8);

    // Create bufferAllocInfo
    var bufferAllocInfo = new BufferAllocInfo {
      Usage = BufferUsageFlags.TransferSrcBit,
      Properties = MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit
    };

    Buffer stagingBuffer = default;
    DeviceMemory stagingBufferMemory = default;
    // Create staging buffer.
    bufferUtils.CreateBuffer(imageByteSize, bufferAllocInfo, ref stagingBuffer, ref stagingBufferMemory);

    void* imgData;
    data.vk.MapMemory(data.Device, stagingBufferMemory, 0, imageByteSize, 0, &imgData);
    rawImage.CopyPixelDataTo(new Span<byte>(imgData, (int)imageByteSize));
    data.vk.UnmapMemory(data.Device, stagingBufferMemory);

    TransitionImageLayout(image, Format.R8G8B8A8Unorm, ImageLayout.Undefined, ImageLayout.TransferDstOptimal);
    CopyBufferToImage(stagingBuffer, image, (uint)rawImage.Width, (uint)rawImage.Height);
    TransitionImageLayout(image, Format.R8G8B8A8Unorm, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);

    data.vk.DestroyBuffer(data.Device, stagingBuffer, null);
    data.vk.FreeMemory(data.Device, stagingBufferMemory, null);
  }

  private unsafe void CreateImage(Image<Rgba32> rawImage, ref Image image, ref DeviceMemory imageMemory) {
    var imageByteSize = (ulong)(rawImage.Width * rawImage.Height * rawImage.PixelType.BitsPerPixel / 8);

    // Create bufferAllocInfo
    var bufferAllocInfo = new BufferAllocInfo {
      Usage = BufferUsageFlags.TransferSrcBit,
      Properties = MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit
    };

    Buffer stagingBuffer = default;
    DeviceMemory stagingBufferMemory = default;
    // Create staging buffer.
    bufferUtils.CreateBuffer(imageByteSize, bufferAllocInfo, ref stagingBuffer, ref stagingBufferMemory);

    void* imgData;
    data.vk.MapMemory(data.Device, stagingBufferMemory, 0, imageByteSize, 0, &imgData);
    rawImage.CopyPixelDataTo(new Span<byte>(imgData, (int)imageByteSize));
    data.vk.UnmapMemory(data.Device, stagingBufferMemory);

    CreateImage((uint)rawImage.Width, (uint)rawImage.Height, Format.R8G8B8A8Unorm, ImageTiling.Optimal, ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit, MemoryPropertyFlags.DeviceLocalBit, ref image, ref imageMemory);

    TransitionImageLayout(image, Format.R8G8B8A8Unorm, ImageLayout.Undefined, ImageLayout.TransferDstOptimal);
    CopyBufferToImage(stagingBuffer, image, (uint)rawImage.Width, (uint)rawImage.Height);
    TransitionImageLayout(image, Format.R8G8B8A8Unorm, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);

    data.vk.DestroyBuffer(data.Device, stagingBuffer, null);
    data.vk.FreeMemory(data.Device, stagingBufferMemory, null);
  }

  // public unsafe void CreateTexture(string textureName, ref Image image, ref DeviceMemory imageMemory) {
  //   var fullPath = Path.Join(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"./resources/textures/{textureName}");

  //   if (!File.Exists(fullPath)) {
  //     throw new FileNotFoundException($"Texture {textureName} does not exist at {fullPath}");
  //   }

  //   // Load image from path.
  //   var rawImage = SKImage.FromEncodedData(fullPath);
  //   SKData imageData = rawImage.EncodedData;

  //   var imageByteSize = rawImage.Info.BytesSize;

  //   // Create bufferAllocInfo
  //   var bufferAllocInfo = new BufferAllocInfo {
  //     Usage = BufferUsageFlags.TransferSrcBit,
  //     SharingMode = SharingMode.Exclusive,
  //     Properties = MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit
  //   };

  //   Buffer stagingBuffer = default;
  //   DeviceMemory stagingBufferMemory = default;
  //   // Create staging buffer.
  //   bufferUtils.CreateBuffer((ulong)imageByteSize, bufferAllocInfo, ref stagingBuffer, ref stagingBufferMemory);

  //   void* imgData;
  //   data.vk.MapMemory(data.Device, stagingBufferMemory, 0, (ulong)imageData.Size, 0, &imgData);
  //   Unsafe.CopyBlock(imgData, (void*)imageData.Data, (uint)imageData.Size);
  //   data.vk.UnmapMemory(data.Device, stagingBufferMemory);

  //   CreateImage((uint)rawImage.Width, (uint)rawImage.Height, Format.R8G8B8A8Unorm, ImageTiling.Optimal, ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit, MemoryPropertyFlags.DeviceLocalBit, ref image, ref imageMemory);

  //   TransitionImageLayout(image, Format.R8G8B8A8Unorm, ImageLayout.Undefined, ImageLayout.TransferDstOptimal);
  //   CopyBufferToImage(stagingBuffer, image, (uint)rawImage.Width, (uint)rawImage.Height);
  //   TransitionImageLayout(image, Format.R8G8B8A8Unorm, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);

  //   data.vk.DestroyBuffer(data.Device, stagingBuffer, null);
  //   data.vk.FreeMemory(data.Device, stagingBufferMemory, null);
  // }

  public unsafe void CreateSampler(ref Sampler sampler) {
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
      MipLodBias = 0,
      MinLod = 0,
      MaxLod = 0
    };

    if (data.vk.CreateSampler(data.Device, &samplerInfo, null, out sampler) != Result.Success) {
      throw new Exception("failed to create sampler!");
    }
  }

  public unsafe void CreateImageView(Image image, ref ImageView imageView) {
    imageView = CreateImageView(image, Format.R8G8B8A8Unorm, ImageAspectFlags.ColorBit);
  }

  // CreateImageView: Create an image view.
  public unsafe ImageView CreateImageView(Image image, Format format, ImageAspectFlags aspectFlags) {
    var imageViewCreateInfo = new ImageViewCreateInfo {
      SType = StructureType.ImageViewCreateInfo,
      Image = image,
      ViewType = ImageViewType.Type2D,
      Format = format,
      SubresourceRange = new ImageSubresourceRange {
        AspectMask = aspectFlags,
        BaseMipLevel = 0,
        LevelCount = 1,
        BaseArrayLayer = 0,
        LayerCount = 1
      }
    };

    if (data.vk.CreateImageView(data.Device, &imageViewCreateInfo, null, out var imageView) != Result.Success) {
      throw new Exception("failed to create image views!");
    }

    return imageView;
  }

  private unsafe void TransitionImageLayout(Image image, Format format, ImageLayout oldLayout, ImageLayout newLayout) {
    var commandBuffer = commandUtils.BeginSingleTimeCommands();

    var barrier = new ImageMemoryBarrier {
      SType = StructureType.ImageMemoryBarrier,
      OldLayout = oldLayout,
      NewLayout = newLayout,
      SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
      DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
      Image = image,
      SubresourceRange = new ImageSubresourceRange {
        AspectMask = ImageAspectFlags.ColorBit,
        BaseMipLevel = 0,
        LevelCount = 1,
        BaseArrayLayer = 0,
        LayerCount = 1
      }
    };

    PipelineStageFlags sourceStage;
    PipelineStageFlags destinationStage;

    if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.TransferDstOptimal) {
      barrier.SrcAccessMask = 0;
      barrier.DstAccessMask = AccessFlags.TransferWriteBit;

      sourceStage = PipelineStageFlags.TopOfPipeBit;
      destinationStage = PipelineStageFlags.TransferBit;
    }
    else if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal) {
      barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
      barrier.DstAccessMask = AccessFlags.ShaderReadBit;

      sourceStage = PipelineStageFlags.TransferBit;
      destinationStage = PipelineStageFlags.FragmentShaderBit;
    }
    else {
      throw new Exception("unsupported layout transition!");
    }

    data.vk.CmdPipelineBarrier(
      commandBuffer,
      sourceStage,
      destinationStage,
      0,
      0,
      null,
      0,
      null,
      1,
      &barrier
    );

    commandUtils.EndSingleTimeCommands(commandBuffer);
  }

  private unsafe void CopyBufferToImage(Buffer buffer, Image image, uint width, uint height) {
    var commandBuffer = commandUtils.BeginSingleTimeCommands();

    var region = new BufferImageCopy {
      BufferOffset = 0,
      BufferRowLength = 0,
      BufferImageHeight = 0,
      ImageSubresource = new ImageSubresourceLayers {
        AspectMask = ImageAspectFlags.ColorBit,
        MipLevel = 0,
        BaseArrayLayer = 0,
        LayerCount = 1
      },
      ImageOffset = new Offset3D {
        X = 0,
        Y = 0,
        Z = 0
      },
      ImageExtent = new Extent3D {
        Width = width,
        Height = height,
        Depth = 1
      }
    };

    data.vk.CmdCopyBufferToImage(commandBuffer, buffer, image, ImageLayout.TransferDstOptimal, 1, &region);

    commandUtils.EndSingleTimeCommands(commandBuffer);
  }

  public unsafe void CreateImage(uint width, uint height, Format format, ImageTiling tiling, ImageUsageFlags usage, MemoryPropertyFlags properties, ref Image image, ref DeviceMemory imageMemory) {
    var imageInfo = new ImageCreateInfo {
      SType = StructureType.ImageCreateInfo,
      ImageType = ImageType.Type2D,
      Extent = new Extent3D {
        Width = width,
        Height = height,
        Depth = 1
      },
      MipLevels = 1,
      ArrayLayers = 1,
      Format = format,
      Tiling = tiling,
      InitialLayout = ImageLayout.Undefined,
      Usage = usage,
      SharingMode = SharingMode.Exclusive,
      Samples = SampleCountFlags.Count1Bit,
      Flags = 0
    };

    fixed (Image* imagePtr = &image) {
      if (data.vk.CreateImage(data.Device, &imageInfo, null, imagePtr) != Result.Success) {
        throw new Exception("failed to create image!");
      }
    }

    data.vk.GetImageMemoryRequirements(data.Device, image, out MemoryRequirements memRequirements);

    var allocInfo = new MemoryAllocateInfo {
      SType = StructureType.MemoryAllocateInfo,
      AllocationSize = memRequirements.Size,
      MemoryTypeIndex = bufferUtils.FindMemoryType(memRequirements.MemoryTypeBits, properties)
    };

    fixed (DeviceMemory* imageMemoryPtr = &imageMemory) {
      if (data.vk.AllocateMemory(data.Device, &allocInfo, null, imageMemoryPtr) != Result.Success) {
        throw new Exception("failed to allocate image memory!");
      }
    }

    data.vk.BindImageMemory(data.Device, image, imageMemory, 0);
  }

  public Format FindDepthFormat() {
    return FindSupportedFormat(new[] { Format.D32Sfloat, Format.D32SfloatS8Uint, Format.D24UnormS8Uint }, ImageTiling.Optimal, FormatFeatureFlags.DepthStencilAttachmentBit);
  }

  private Format FindSupportedFormat(IEnumerable<Format> candidates, ImageTiling tiling, FormatFeatureFlags features) {
    foreach (var format in candidates) {
      data.vk.GetPhysicalDeviceFormatProperties(data.PhysicalDevice, format, out var props);

      if (tiling == ImageTiling.Linear && (props.LinearTilingFeatures & features) == features) {
        return format;
      }
      else if (tiling == ImageTiling.Optimal && (props.OptimalTilingFeatures & features) == features) {
        return format;
      }
    }

    throw new Exception("failed to find supported format!");
  }
}