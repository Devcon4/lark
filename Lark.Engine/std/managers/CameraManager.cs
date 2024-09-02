using Lark.Engine.ecs;
using Lark.Engine.model;
using Lark.Engine.pipeline;

namespace Lark.Engine.std;

public class CameraManager(LarkVulkanData data) : LarkManager {
  public Dictionary<Guid, LarkCamera> Cameras => data.cameras;

  public LarkCamera? ActiveCamera => data.cameras.Values.Cast<LarkCamera?>().DefaultIfEmpty(null).FirstOrDefault(c => c.HasValue && c.Value.Active);
}