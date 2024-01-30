namespace Lark.Engine.physx.managers;

public class LarkPhysxConfig
{
  public bool PVDEnable { get; set; } = false;
  public string? PVDHost { get; set; }
  public int? PVDPort { get; set; }
}