namespace Lark.Engine.model;

public record struct LarkInstance() {
  public Guid InstanceId = Guid.NewGuid();
  public LarkTransform Transform;
  public Guid ModelId;
}
