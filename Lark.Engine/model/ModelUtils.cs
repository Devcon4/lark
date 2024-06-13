using System.Reflection;
using SharpGLTF.Schema2;
using Lark.Engine.pipeline;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using System.Runtime.InteropServices;
using Buffer = Silk.NET.Vulkan.Buffer;
using System.Runtime.CompilerServices;
using System.Numerics;
using Lark.Engine.std;

namespace Lark.Engine.Model;

public class ModelUtils(LarkVulkanData data, ImageUtils imageUtils, BufferUtils bufferUtils, ILogger<ModelUtils> logger, PBRPipeline pipeline) {


  public LarkModel LoadFile(string modelName) {
    logger.LogDebug("{modelName}:: Begin loading...", modelName);
    var fullPath = Path.Join(Path.GetDirectoryName(AppContext.BaseDirectory), $"./resources/models/{modelName}");

    if (!File.Exists(fullPath)) {
      throw new FileNotFoundException($"Model {modelName} does not exist at {fullPath}");
    }

    var model = ModelRoot.Load(fullPath);
    var larkModel = new LarkModel();

    // LoadTheNodes in the defaultScene.
    var Nodes = new List<LarkNode>();
    foreach (var child in model.DefaultScene.VisualChildren) {
      Nodes.Add(LoadNode(child, larkModel));
    }

    larkModel.Nodes = Nodes.ToArray().AsMemory();
    logger.LogDebug("{modelName}:: Loaded nodes {nodeCount}", modelName, Nodes.Count);
    larkModel.Images = LoadImages(model);
    logger.LogDebug("{modelName}:: Loaded images {imageCount}", modelName, larkModel.Images.Length);
    larkModel.Textures = LoadTextures(model);
    logger.LogDebug("{modelName}:: Loaded textures {textureCount}", modelName, larkModel.Textures.Length);
    larkModel.Materials = LoadMaterials(model);
    logger.LogDebug("{modelName}:: Loaded materials {materialCount}", modelName, larkModel.Materials.Length);

    CreateDescriptorPool(larkModel);
    logger.LogDebug("{modelName}:: Created descriptor pool.", modelName);
    CreateDescriptorSets(larkModel);
    logger.LogDebug("{modelName}:: Created descriptor sets.", modelName);
    CreateMeshBuffers(larkModel);
    logger.LogDebug("{modelName}:: Created mesh buffers.", modelName);

    logger.LogDebug("{modelName}:: Complete!", modelName);
    return larkModel;
  }

  public unsafe void CreateMeshBuffers(LarkModel model) {
    var vertexBufferSize = (ulong)(Marshal.SizeOf<LarkVertex>() * model.meshVertices.Count);
    var verticiesStagingBuffer = default(Buffer);
    var verticiesStagingBufferMemory = default(DeviceMemory);

    bufferUtils.CreateBuffer(
      vertexBufferSize,
      new BufferAllocInfo {
        Usage = BufferUsageFlags.TransferSrcBit,
        Properties = MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit
      },
      ref verticiesStagingBuffer,
      ref verticiesStagingBufferMemory
    );

    var verticesSpan = model.meshVertices.ToArray().AsSpan();

    void* verticiesPtr;
    data.vk.MapMemory(data.Device, verticiesStagingBufferMemory, 0, vertexBufferSize, 0, &verticiesPtr);
    verticesSpan.CopyTo(new Span<LarkVertex>(verticiesPtr, model.meshVertices.Count));
    data.vk.UnmapMemory(data.Device, verticiesStagingBufferMemory);

    bufferUtils.CreateBuffer(
      vertexBufferSize,
      new BufferAllocInfo {
        Usage = BufferUsageFlags.TransferDstBit | BufferUsageFlags.VertexBufferBit,
        Properties = MemoryPropertyFlags.DeviceLocalBit
      },
      ref model.Vertices.Buffer,
      ref model.Vertices.Memory
    );

    bufferUtils.CopyBuffer(verticiesStagingBuffer, model.Vertices.Buffer, vertexBufferSize);
    data.vk.DestroyBuffer(data.Device, verticiesStagingBuffer, null);
    data.vk.FreeMemory(data.Device, verticiesStagingBufferMemory, null);

    var indexBufferSize = (ulong)(Marshal.SizeOf<ushort>() * model.meshIndices.Count);
    var indiciesStagingBuffer = default(Buffer);
    var indiciesStagingBufferMemory = default(DeviceMemory);

    bufferUtils.CreateBuffer(
      indexBufferSize,
      new BufferAllocInfo {
        Usage = BufferUsageFlags.TransferSrcBit,
        Properties = MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit
      },
      ref indiciesStagingBuffer,
      ref indiciesStagingBufferMemory
    );

    var indiciesSpan = model.meshIndices.ToArray().AsSpan();

    void* indiciesPtr;
    data.vk.MapMemory(data.Device, indiciesStagingBufferMemory, 0, indexBufferSize, 0, &indiciesPtr);
    indiciesSpan.CopyTo(new Span<ushort>(indiciesPtr, model.meshIndices.Count));
    data.vk.UnmapMemory(data.Device, indiciesStagingBufferMemory);

    bufferUtils.CreateBuffer(
      indexBufferSize,
      new BufferAllocInfo {
        Usage = BufferUsageFlags.TransferDstBit | BufferUsageFlags.IndexBufferBit,
        Properties = MemoryPropertyFlags.DeviceLocalBit
      },
      ref model.Indices.Buffer,
      ref model.Indices.Memory
    );

    bufferUtils.CopyBuffer(indiciesStagingBuffer, model.Indices.Buffer, indexBufferSize);
    data.vk.DestroyBuffer(data.Device, indiciesStagingBuffer, null);
    data.vk.FreeMemory(data.Device, indiciesStagingBufferMemory, null);

    // Clear the mesh data.
    // model.meshVertices.Clear();
    // model.meshIndices.Clear();
  }

  // private unsafe void ModelDescriptorLayouts(out DescriptorSetLayout descriptorSetLayouts) {
  //   var uboLayoutBinding = new DescriptorSetLayoutBinding {
  //     Binding = 0,
  //     DescriptorCount = 1,
  //     DescriptorType = DescriptorType.UniformBuffer,
  //     PImmutableSamplers = null,
  //     StageFlags = ShaderStageFlags.VertexBit
  //   };

  //   var samplerLayoutBinding = new DescriptorSetLayoutBinding {
  //     Binding = 1,
  //     DescriptorCount = 1,
  //     DescriptorType = DescriptorType.CombinedImageSampler,
  //     PImmutableSamplers = null,
  //     StageFlags = ShaderStageFlags.FragmentBit
  //   };

  //   var normalSamplerLayoutBinding = new DescriptorSetLayoutBinding {
  //     Binding = 2,
  //     DescriptorCount = 1,
  //     DescriptorType = DescriptorType.CombinedImageSampler,
  //     PImmutableSamplers = null,
  //     StageFlags = ShaderStageFlags.FragmentBit
  //   };

  //   var bindings = new[] { uboLayoutBinding, samplerLayoutBinding, normalSamplerLayoutBinding };
  //   var handler = bindings.AsMemory().Pin();

  //   var layoutInfo = new DescriptorSetLayoutCreateInfo {
  //     SType = StructureType.DescriptorSetLayoutCreateInfo,
  //     BindingCount = (uint)bindings.Length,
  //     PBindings = (DescriptorSetLayoutBinding*)handler.Pointer
  //   };

  //   if (data.vk.CreateDescriptorSetLayout(data.Device, &layoutInfo, null, out descriptorSetLayouts) != Result.Success) {
  //     throw new Exception("failed to create descriptor set layout!");
  //   }

  //   logger.LogInformation("Created descriptor set layout.");
  // }

  // public unsafe void Update(LarkModel model, int index) {
  //   for (var i = 0; i < model.Nodes.Length; i++) {
  //     UpdateNode(model.Nodes.Span[i], model.Transform, index);
  //   }
  // }

  // private unsafe void UpdateNode(LarkNode node, LarkTransform transform, int index) {
  //   var absoluteTransform = node.Transform * transform;
  //   foreach (var child in node.Children) {
  //     UpdateNode(child, node.Transform, index);
  //   }

  //   var absoluteMatrix = absoluteTransform.ToMatrix().ToSystem();

  //   // Setup the push constants.
  //   data.vk.CmdPushConstants(
  //     data.CommandBuffers[index],
  //     renderer.PipelineLayout,
  //     ShaderStageFlags.VertexBit,
  //     0,
  //     (uint)Marshal.SizeOf<Matrix4x4>(),
  //     &absoluteMatrix
  //   );

  // }



  // Create the descriptor pool for the model.
  public unsafe void CreateDescriptorPool(LarkModel model) {
    var poolSizes = new DescriptorPoolSize[] {
      new() {
        Type = DescriptorType.UniformBuffer,
        DescriptorCount = (uint)LarkVulkanData.MaxFramesInFlight
      },
      new() {
        Type = DescriptorType.CombinedImageSampler,
        DescriptorCount = (uint)model.Images.Length * LarkVulkanData.MaxFramesInFlight
      }
    };

    // maxSizes is the total number of descriptor sets that can be allocated from the pool.
    // It is the number of images plus one for the uniform buffer times the number of frames in flight.
    var maxSizes = (1 + (uint)model.Images.Length) * LarkVulkanData.MaxFramesInFlight;

    var poolInfo = new DescriptorPoolCreateInfo {
      SType = StructureType.DescriptorPoolCreateInfo,
      PoolSizeCount = (uint)poolSizes.Length,
      PPoolSizes = (DescriptorPoolSize*)poolSizes.AsMemory().Pin().Pointer,
      MaxSets = maxSizes
    };

    if (data.vk.CreateDescriptorPool(data.Device, &poolInfo, null, out model.DescriptorPool) != Result.Success) {
      throw new Exception("failed to create descriptor pool!");
    }
  }

  // Create the descriptor set layout for the model. Makes a binding for the uniform buffer and each image.

  // TODO: ModelUtils needs to know what DescriptorSetLayout to use. If we ref PBRPipeline here, we create a circular reference. Refactor this to be more elegant.
  public unsafe void CreateDescriptorSets(LarkModel model) {

    var matrixLayouts = Enumerable.Repeat(pipeline.Data.DescriptorSetLayouts[PBRPipeline.Layouts.Matricies], LarkVulkanData.MaxFramesInFlight).ToArray().AsMemory();
    var handler = matrixLayouts.Pin();

    // for each MaxFramesInFlight, create the ubo descriptor set.
    for (var i = 0; i < LarkVulkanData.MaxFramesInFlight; i++) {
      var matrixAllocInfo = new DescriptorSetAllocateInfo {
        SType = StructureType.DescriptorSetAllocateInfo,
        DescriptorPool = model.DescriptorPool,
        DescriptorSetCount = 1,
        PSetLayouts = (DescriptorSetLayout*)handler.Pointer
      };

      if (data.vk.AllocateDescriptorSets(data.Device, &matrixAllocInfo, out model.MatrixDescriptorSet.Span[i]) != Result.Success) {
        throw new Exception("failed to allocate descriptor sets!");
      }

      var bufferInfo = new DescriptorBufferInfo {
        Buffer = data.UniformBuffers[i].Buffer,
        Offset = 0,
        Range = (ulong)Marshal.SizeOf<UniformBufferObject>()
      };

      var uboDescriptorSet = new WriteDescriptorSet {
        SType = StructureType.WriteDescriptorSet,
        DstSet = model.MatrixDescriptorSet.Span[i],
        DstBinding = 0,
        DstArrayElement = 0,
        DescriptorType = DescriptorType.UniformBuffer,
        DescriptorCount = 1,
        PBufferInfo = &bufferInfo
      };

      data.vk.UpdateDescriptorSets(data.Device, 1, &uboDescriptorSet, 0, null);
    }

    // for each MaxFramesInFlight, create the ubo descriptor set.
    for (var j = 0; j < model.Images.Length; j++) {
      var textureLayouts = Enumerable.Repeat(pipeline.Data.DescriptorSetLayouts[PBRPipeline.Layouts.Textures], LarkVulkanData.MaxFramesInFlight).ToArray().AsMemory();
      var texturesHandler = textureLayouts.Pin();

      var textureAllocInfo = new DescriptorSetAllocateInfo {
        SType = StructureType.DescriptorSetAllocateInfo,
        DescriptorPool = model.DescriptorPool,
        DescriptorSetCount = LarkVulkanData.MaxFramesInFlight,
        PSetLayouts = (DescriptorSetLayout*)texturesHandler.Pointer
      };

      var descriptorSets = new DescriptorSet[LarkVulkanData.MaxFramesInFlight];
      if (data.vk.AllocateDescriptorSets(data.Device, &textureAllocInfo, descriptorSets) != Result.Success) {
        throw new Exception("failed to allocate descriptor sets!");
      }

      for (var i = 0; i < LarkVulkanData.MaxFramesInFlight; i++) {
        var imageInfo = new DescriptorImageInfo {
          Sampler = model.Images.Span[j].Sampler,
          ImageView = model.Images.Span[j].View,
          ImageLayout = ImageLayout.ShaderReadOnlyOptimal
        };

        var materialDescriptorSet = new WriteDescriptorSet {
          SType = StructureType.WriteDescriptorSet,
          DstSet = descriptorSets[i],
          DstBinding = 0,
          DstArrayElement = 0,
          DescriptorType = DescriptorType.CombinedImageSampler,
          DescriptorCount = 1,
          PImageInfo = &imageInfo
        };

        data.vk.UpdateDescriptorSets(data.Device, 1, &materialDescriptorSet, 0, null);

        model.Images.Span[j].DescriptorSets = descriptorSets;
      }
    }
  }

  // public unsafe void CreateDescriptorSets(LarkModel model) {

  //   fixed (DescriptorSetLayout* descriptorSetLayoutPtr = &data.DescriptorSetLayout) {

  //     var allocInfo = new DescriptorSetAllocateInfo {
  //       SType = StructureType.DescriptorSetAllocateInfo,
  //       DescriptorPool = data.DescriptorPool,
  //       DescriptorSetCount = LarkVulkanData.MaxFramesInFlight,
  //       PSetLayouts = descriptorSetLayoutPtr
  //     };

  //     var descriptorSets = new DescriptorSet[model.Images.Length];
  //     if (data.vk.AllocateDescriptorSets(data.Device, &allocInfo, descriptorSets) != Result.Success) {
  //       throw new Exception("failed to allocate descriptor sets!");
  //     }
  //     for (var i = 0; i < model.Images.Length; i++) {
  //       var imageInfo = new DescriptorImageInfo {
  //         Sampler = model.Images.Span[i].Sampler,
  //         ImageView = model.Images.Span[i].View,
  //         ImageLayout = ImageLayout.ShaderReadOnlyOptimal
  //       };

  //       var writes = new WriteDescriptorSet[] {
  //         new() {
  //           SType = StructureType.WriteDescriptorSet,
  //           DstSet = descriptorSets[i],
  //           DstBinding = 0,
  //           DstArrayElement = 0,
  //           DescriptorType = DescriptorType.UniformBuffer,
  //           DescriptorCount = 1,
  //           PImageInfo = &imageInfo
  //         }
  //       };

  //       fixed (WriteDescriptorSet* descriptorWritesPtr = writes) {
  //         data.vk.UpdateDescriptorSets(data.Device, (uint)writes.Length, descriptorWritesPtr, 0, null);
  //       }
  //     }
  //   }
  // }

  public Memory<LarkImage> LoadImages(ModelRoot model) {
    var images = new List<LarkImage>();
    foreach (var image in model.LogicalImages) {
      var larkImage = new LarkImage();
      imageUtils.CreateTexture(image.Content.Content, ref larkImage.Image, ref larkImage.Memory);
      imageUtils.CreateImageView(larkImage.Image, ref larkImage.View);
      imageUtils.CreateSampler(ref larkImage.Sampler);
      images.Add(larkImage);
    }

    return images.ToArray().AsMemory();
  }

  public Memory<LarkTexture> LoadTextures(ModelRoot model) {
    var textures = new List<LarkTexture>();

    foreach (var tex in model.LogicalTextures) {
      textures.Add(new LarkTexture {
        TextureIndex = tex.PrimaryImage.LogicalIndex,
        SamplerIndex = tex.Sampler?.LogicalIndex
      });
    }

    return textures.ToArray().AsMemory();
  }

  public Memory<LarkMaterial> LoadMaterials(ModelRoot model) {
    var materials = new List<LarkMaterial>();

    foreach (var mat in model.LogicalMaterials) {
      var diffTex = mat.GetDiffuseTexture();
      var larkMaterial = new LarkMaterial {
        BaseColorTextureIndex = diffTex != null ? diffTex.LogicalIndex : null
      };
      var baseColorFactor = mat?.FindChannel("BaseColorFactor");
      if (baseColorFactor.HasValue) {
        larkMaterial.BaseColorFactor = new Vector4D<float>(
          baseColorFactor.Value.Color.X,
          baseColorFactor.Value.Color.Y,
          baseColorFactor.Value.Color.Z,
          baseColorFactor.Value.Color.W
        );
      }

      materials.Add(larkMaterial);
    }

    return materials.ToArray().AsMemory();
  }



  public LarkNode LoadNode(Node inputNode, LarkModel model) {
    var larkNode = new LarkNode {
      Children = inputNode.VisualChildren.Select(n => LoadNode(n, model)).ToArray()
    };

    var primitives = new List<LarkPrimitive>();

    larkNode.Transform = new(inputNode.LocalTransform);

    if (inputNode.Mesh == null) {
      return larkNode;
    }

    foreach (var primitive in inputNode.Mesh.Primitives) {
      // Get accessor index count.

      var Primitive = new LarkPrimitive() {
        FirstIndex = primitive.IndexAccessor.ByteOffset / 2,
        IndexCount = primitive.IndexAccessor.Count,
        MaterialIndex = primitive.Material.LogicalIndex
      };

      // Get the position attribute accessor.
      var posAccessor = primitive.GetVertexAccessor("POSITION");
      var posData = posAccessor.AsVector3Array().ToArray();

      // Get the normal attribute accessor.
      var normAccessor = primitive.GetVertexAccessor("NORMAL");
      var normData = normAccessor.AsVector3Array().ToArray();

      // Get the texture coordinate attribute accessor.
      var texAccessor = primitive.GetVertexAccessor("TEXCOORD_0");
      var texData = texAccessor.AsVector2Array().ToArray();

      // Build the vertices.
      for (var i = 0; i < posData.Length; i++) {
        model.meshVertices.Add(new LarkVertex {
          Pos = new Vector3D<float>(posData[i].X, posData[i].Y, posData[i].Z),
          Normal = new Vector3D<float>(normData[i].X, normData[i].Y, normData[i].Z),
          UV = new Vector2D<float>(texData[i].X, texData[i].Y),
          Color = new Vector3D<float>(1, 1, 1)
        });
      }

      // Get the index accessor.
      var indexData = primitive.GetIndices();

      // Build the indicies.
      foreach (var i in indexData) {
        model.meshIndices.Add(Convert.ToUInt16(i));
        if (i != (ushort)i) {
          logger.LogError("Index {i} is not a ushort!", i);
        }
      }

      primitives.Add(Primitive);
    }

    larkNode.Primitives = primitives.ToArray();

    return larkNode;
  }
}
