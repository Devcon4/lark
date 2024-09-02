using System.Buffers;
using Silk.NET.Vulkan;

namespace Lark.Engine.pipeline;


public interface ILarkPipeline {
  void Start();
  void Draw(uint index);
  void Update(uint index);
  void Cleanup();

  public abstract int Priority { get; }
  PipelineData Data { get; set; }
}

public record LarkLayoutBindingInfo(DescriptorType Type, ShaderStageFlags Stages, uint Ordinal);

public class LarkPipelineSet(uint Ordinal, IEnumerable<LarkLayoutBindingInfo> LayoutBindings) {
  public DescriptorSetLayout Layout;
  public Memory<DescriptorSet> Sets = new(new DescriptorSet[LarkVulkanData.MaxFramesInFlight]);
  public readonly int Count = LayoutBindings.Count();
  public readonly IEnumerable<LarkLayoutBindingInfo> LayoutBindings = LayoutBindings;
  public readonly uint Ordinal = Ordinal;
}

public class PipelineData {
  public RenderPass RenderPass;
  public PipelineLayout PipelineLayout;
  public Pipeline Pipeline;
  public ClearValue[] clearValues = [];
  public Framebuffer[] Framebuffers = [];

  [Obsolete("Use PipelineSets instead")]
  public Dictionary<string, DescriptorSetLayout> DescriptorSetLayouts = [];
  public DescriptorPool DescriptorPool;
  public Dictionary<string, LarkPipelineSet> PipelineSets = [];
}

public abstract class LarkPipeline(LarkVulkanData shareData) : ILarkPipeline {
  public PipelineData Data { get; set; } = new();
  public HashSet<MemoryHandle> MemoryHandles { get; set; } = [];

  public virtual int Priority => 1024;
  public virtual void Start() { }
  public virtual void Draw(uint index) { }
  public virtual void Update(uint index) { }
  public virtual void Cleanup() {
    foreach (var handle in MemoryHandles) {
      handle.Dispose();
    }
  }

  protected unsafe void UpdateSet(string name, uint index, WriteDescriptorSet write) {
    if (!Data.PipelineSets.TryGetValue(name, out var set)) {
      throw new Exception($"Descriptor set {name} not found");
    }

    var setsHandler = set.Sets.Pin();
    write.DstSet = set.Sets.Span[(int)index];

    shareData.vk.UpdateDescriptorSets(shareData.Device, 1, &write, 0, null);

    setsHandler.Dispose();
  }

  protected unsafe void BindSet(string name, uint frameIndex, int? setIndex = null, uint? firstSet = null) {
    if (!Data.PipelineSets.TryGetValue(name, out var set)) {
      throw new Exception($"Descriptor set {name} not found");
    }

    setIndex ??= (int)frameIndex;
    firstSet ??= set.Ordinal;

    var setsHandler = set.Sets.Pin();
    var sets = (DescriptorSet*)setsHandler.Pointer + setIndex.Value;
    shareData.vk.CmdBindDescriptorSets(
      shareData.CommandBuffers[frameIndex],
      PipelineBindPoint.Graphics,
      Data.PipelineLayout,
      firstSet.Value,
      1,
      sets,
      0,
      null
    );

    setsHandler.Dispose();
  }

  protected void RegisterSet(string name, uint ordinal, IEnumerable<LarkLayoutBindingInfo> layoutBindings) {
    if (!Data.PipelineSets.TryAdd(name, new LarkPipelineSet(ordinal, layoutBindings))) {
      throw new Exception($"Descriptor set {name} already exists");
    }
  }

  protected unsafe void CreateSet(string name, int count = LarkVulkanData.MaxFramesInFlight) {
    if (!Data.PipelineSets.TryGetValue(name, out var set)) {
      throw new Exception($"Descriptor set {name} not found");
    }

    set.Sets = new(new DescriptorSet[count]);

    var setsHandler = set.Sets.Pin();

    var layoutMem = new Memory<DescriptorSetLayout>(Enumerable.Repeat(set.Layout, count).ToArray());
    var layoutHandle = layoutMem.Pin();

    var allocInfo = new DescriptorSetAllocateInfo {
      SType = StructureType.DescriptorSetAllocateInfo,
      DescriptorPool = Data.DescriptorPool,
      DescriptorSetCount = (uint)count,
      PSetLayouts = (DescriptorSetLayout*)layoutHandle.Pointer
    };

    if (shareData.vk.AllocateDescriptorSets(shareData.Device, &allocInfo, (DescriptorSet*)setsHandler.Pointer) != Result.Success) {
      throw new Exception("failed to allocate descriptor sets!");
    }

    // set.Sets.Span[(int)frameIndex * set.Count] = finalSet;

    layoutHandle.Dispose();
    setsHandler.Dispose();
  }

  protected unsafe void CreateSetLayouts() {
    foreach (var set in Data.PipelineSets) {
      var bindings = set.Value.LayoutBindings.Select((binding, index) => new DescriptorSetLayoutBinding {
        Binding = (uint)binding.Ordinal,
        DescriptorType = binding.Type,
        DescriptorCount = 1,
        StageFlags = binding.Stages
      }).ToArray();

      var bindingsMem = new Memory<DescriptorSetLayoutBinding>(bindings);
      var bindingsHandle = bindingsMem.Pin();

      var createInfo = new DescriptorSetLayoutCreateInfo {
        SType = StructureType.DescriptorSetLayoutCreateInfo,
        BindingCount = (uint)bindingsMem.Length,
        PBindings = (DescriptorSetLayoutBinding*)bindingsHandle.Pointer
      };

      if (shareData.vk.CreateDescriptorSetLayout(shareData.Device, &createInfo, null, out var Layout) != Result.Success) {
        throw new Exception("failed to create descriptor set layout!");
      }

      Data.PipelineSets[set.Key].Layout = Layout;
      bindingsHandle.Dispose();
    }
  }


  protected (MemoryHandle, uint) RegisterMemory<T>(T[] data) {
    var mem = new Memory<T>(data);
    var memHandle = mem.Pin();
    MemoryHandles.Add(memHandle);
    return (memHandle, (uint)mem.Length);
  }

  protected unsafe ShaderModule CreateShaderModule(byte[] code) {
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