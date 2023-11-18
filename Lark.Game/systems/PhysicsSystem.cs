using System.Collections.Frozen;
using System.Numerics;
using Lark.Engine.ecs;
using Lark.Engine.std;
using Lark.Game.components;

namespace Lark.Game.systems;

public class PhysicsSystem(EntityManager em, TimeManager tm) : LarkSystem {
  public override Type[] RequiredComponents => new Type[] { typeof(TransformComponent), typeof(ForceComponent) };

  public override Task Init() {
    var start = new TransformComponent(new(10, 0, 0), Vector3.One, Quaternion.Identity);
    var initForce = new ForceComponent(new(0.01f, 0, 0));

    em.AddEntity(new MeshComponent("antiqueCamera/AntiqueCamera.gltf"), new MetadataComponent("Camera-1"), initForce, start with { Position = new(15, 1, -1) });
    em.AddEntity(new MeshComponent("antiqueCamera/AntiqueCamera.gltf"), new MetadataComponent("Camera-1"), initForce, start with { Position = new(15, 1, 0) });
    em.AddEntity(new MeshComponent("antiqueCamera/AntiqueCamera.gltf"), new MetadataComponent("Camera-1"), initForce, start with { Position = new(15, 1, 1) });
    em.AddEntity(new MeshComponent("antiqueCamera/AntiqueCamera.gltf"), new MetadataComponent("Camera-1"), initForce, start with { Position = new(15, 1, 2) });

    em.AddEntity(new MeshComponent("antiqueCamera/AntiqueCamera.gltf"), new MetadataComponent("Camera-1"), initForce, start with { Position = new(15, 0, -1) });
    em.AddEntity(new MeshComponent("antiqueCamera/AntiqueCamera.gltf"), new MetadataComponent("Camera-1"), initForce, start with { Position = new(15, 0, 0) });
    em.AddEntity(new MeshComponent("antiqueCamera/AntiqueCamera.gltf"), new MetadataComponent("Camera-1"), initForce, start with { Position = new(15, 0, 1) });
    em.AddEntity(new MeshComponent("antiqueCamera/AntiqueCamera.gltf"), new MetadataComponent("Camera-1"), initForce, start with { Position = new(15, 0, 2) });

    em.AddEntity(new MeshComponent("antiqueCamera/AntiqueCamera.gltf"), new MetadataComponent("Camera-1"), initForce, start with { Position = new(15, -1, -1) });
    em.AddEntity(new MeshComponent("antiqueCamera/AntiqueCamera.gltf"), new MetadataComponent("Camera-1"), initForce, start with { Position = new(15, -1, 0) });
    em.AddEntity(new MeshComponent("antiqueCamera/AntiqueCamera.gltf"), new MetadataComponent("Camera-1"), initForce, start with { Position = new(15, -1, 1) });
    em.AddEntity(new MeshComponent("antiqueCamera/AntiqueCamera.gltf"), new MetadataComponent("Camera-1"), initForce, start with { Position = new(15, -1, 2) });

    em.AddEntity(new MeshComponent("antiqueCamera/AntiqueCamera.gltf"), new MetadataComponent("Camera-1"), initForce, start with { Position = new(15, -2, -1) });
    em.AddEntity(new MeshComponent("antiqueCamera/AntiqueCamera.gltf"), new MetadataComponent("Camera-1"), initForce, start with { Position = new(15, -2, 0) });
    em.AddEntity(new MeshComponent("antiqueCamera/AntiqueCamera.gltf"), new MetadataComponent("Camera-1"), initForce, start with { Position = new(15, -2, 1) });
    em.AddEntity(new MeshComponent("antiqueCamera/AntiqueCamera.gltf"), new MetadataComponent("Camera-1"), initForce, start with { Position = new(15, -2, 2) });

    var amount = 1;
    for (int j = -amount; j < amount; j++) {
      for (int i = -amount; i < amount; i++) {
        em.AddEntity(new MeshComponent("damagedHelmet/DamagedHelmet.glb"), new MetadataComponent($"Helmet-{j}-{i}"), initForce, start with { Position = new(10, i, j) });
      }
    }

    return Task.CompletedTask;
  }

  public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
    var (key, components) = Entity;
    var (transform, force) = components.Get<TransformComponent, ForceComponent>();

    if (transform.Position.X > 40 || transform.Position.X < 0) {
      force = force with {
        Force = new(-force.Force.X, force.Force.Y, force.Force.Z)
      };
      em.UpdateEntityComponent(key, force);
    }

    var newTransform = transform with {
      Scale = new(.2f, .2f, .2f),
      Position = transform.Position + force.Force * (float)tm.DeltaTime.TotalMilliseconds,
      Rotation = Quaternion.CreateFromYawPitchRoll(0.01f, 0, 0) * (float)tm.DeltaTime.TotalMilliseconds * transform.Rotation
    };
    // var newTransform = transform with {
    //   Position = transform.Position + force.Force * (float)tm.DeltaTime.TotalMilliseconds
    // };

    em.UpdateEntityComponent(key, newTransform);
  }
}