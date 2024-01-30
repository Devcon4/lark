
using System.Collections.Frozen;
using Lark.Engine.ecs;
using Lark.Engine.physx.components;
using Lark.Engine.physx.managers;
using Lark.Engine.std;

namespace Lark.Engine.physx.systems;

public class PhysxWorldSystem(EntityManager em, PhysxManager physxManager) : LarkSystem {
  public override Type[] RequiredComponents => [typeof(PhysxWorldComponent)];

  // Todo: Temp, should be able to handle multiple worlds and scenes.
  private bool IsCreated = false;

  public override Task Init() {
    em.AddEntity(new MetadataComponent("Physx Entity"),
    // new PhysxWorldComponent("Default", new(0, 0, 0)),
    new PhysxWorldComponent("Default", new(0, 9.81f, 0)),
    new SystemComponent(),
    new PhysxEntityMarker());
    return Task.CompletedTask;
  }

  public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
    var (id, components) = Entity;
    var worldComponent = components.Get<PhysxWorldComponent>();
    if (!IsCreated) {
      physxManager.BuildPhysxWorld(worldComponent.WorldName);
      IsCreated = true;
    }
  }
}