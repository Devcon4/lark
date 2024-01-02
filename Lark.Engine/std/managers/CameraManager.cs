using Lark.Engine.Model;
using Lark.Engine.Pipeline;

namespace Lark.Engine.std;

public class CameraManager(LarkVulkanData data) {
  public Dictionary<Guid, LarkCamera> Cameras => data.cameras;

  public LarkCamera ActiveCamera => data.cameras.Values.FirstOrDefault(c => c.Active);
}