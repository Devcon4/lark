using Microsoft.Extensions.Logging;
using Silk.NET.Vulkan;

namespace Lark.Engine.Pipeline;

// Creates normal and texture images and ImageViews.
public class TextureSegment(LarkVulkanData data, ImageUtils imageUtils, ILogger<TextureSegment> logger) {

  public void CreateTextureImage() {
    imageUtils.CreateTexture("owl-1.jpg", out var textureImage, out var textureImageMemory);
    data.TextureImage = textureImage;
    data.TextureImageMemory = textureImageMemory;
  }

  public void CreateTextureImageView() {
    data.TextureImageView = imageUtils.CreateImageView(data.TextureImage, Format.R8G8B8A8Unorm, ImageAspectFlags.ColorBit);
  }
}