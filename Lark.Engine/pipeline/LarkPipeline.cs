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
public class PipelineData {
  public RenderPass RenderPass;
  public PipelineLayout PipelineLayout;
  public Pipeline Pipeline;
  public ClearValue[] clearValues = [];
  public Framebuffer[] Framebuffers = [];
  public Dictionary<string, DescriptorSetLayout> DescriptorSetLayouts = [];
  public DescriptorPool DescriptorPool;
}

public abstract class LarkPipeline : ILarkPipeline {
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

  protected (MemoryHandle, uint) RegisterMemory<T>(T[] data) {
    var mem = new Memory<T>(data);
    var memHandle = mem.Pin();
    MemoryHandles.Add(memHandle);
    return (memHandle, (uint)mem.Length);
  }
}