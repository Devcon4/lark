using System.Collections.Frozen;
using System.Numerics;
using Lark.Engine.ecs;
using Lark.Engine.std;

namespace Lark.Game.components;

public class GravitySystem(EntityManager em) : LarkSystem {
  public override Type[] RequiredComponents => new Type[] { typeof(TransformComponent), typeof(ForceComponent) };

  public override Task Init() {
    em.AddEntity(new TransformComponent(planet, Vector3.One * 2, Quaternion.Identity), new MeshComponent("antiqueCamera/AntiqueCamera.gltf"), new MetadataComponent("Planet-1"));

    return Task.CompletedTask;
  }

  Vector3 planet = new(16, 2, 1);
  float planetMass = 1f;
  float moonMass = 1f;

  public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
    // Adds constant downward force of gravity.
    // var (key, components) = Entity;
    // var (force, transform) = components.Get<ForceComponent, TransformComponent>();

    // var distance = Vector3.Distance(transform.Position, planet);

    // var forceDirection = Vector3.Normalize(transform.Position - planet);
    // var forceMagnitude = (planetMass * moonMass) / (distance * distance);
    // var forceVector = forceMagnitude * forceDirection;

    // force = force with {
    //   Force = forceVector
    // };

    // em.UpdateEntityComponent(key, force);
  }
}