
using System.Collections.Frozen;
using Lark.Engine.ecs;
using Lark.Engine.physx.components;
using Lark.Engine.physx.managers;

namespace Lark.Engine.physx.systems;

public class PhysxMaterialSystem(PhysxManager pm) : LarkSystem {
  public override Type[] RequiredComponents => [typeof(PhysxMaterialComponent)];



  public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
    var (id, components) = Entity;

    // If the entity has a material register it with the PhysxManager and use that material.
    if (components.TryGet<PhysxMaterialComponent>(out var material) && !pm.HasMaterial(id)) {
      pm.RegisterMaterial(id, material.StaticFriction, material.DynamicFriction, material.Restitution);
    }
  }
}