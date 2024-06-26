using Lark.Engine.ecs;
using Lark.Engine.Model;
using Lark.Engine.pipeline;

namespace Lark.Engine.std;

public class CameraManager(LarkVulkanData data) : LarkManager {
  public Dictionary<Guid, LarkCamera> Cameras => data.cameras;

  public LarkCamera ActiveCamera => data.cameras.Values.FirstOrDefault(c => c.Active);
}