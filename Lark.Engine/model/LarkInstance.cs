namespace Lark.Engine.Model;

public record struct LarkInstance()
{
  public Guid InstanceId = Guid.NewGuid();
  public LarkTransform Transform;
  public Guid ModelId;
}
