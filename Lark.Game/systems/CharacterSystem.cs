using System.Collections.Frozen;
using System.Numerics;
using Lark.Engine.ecs;
using Lark.Engine.physx;
using Lark.Engine.physx.managers;
using Lark.Engine.std;
using MagicPhysX;
using Microsoft.Extensions.Logging;

namespace Lark.Game.systems;

public record struct CharacterMovementComponent(float Speed) : ILarkComponent { }

public record struct CharacterDisplacementComponent(Vector3 ForwardDelta, Vector3 BackwardDelta, Vector3 LeftDelta, Vector3 RightDelta) : ILarkComponent {
  public readonly bool Dirty => ForwardDelta != Vector3.Zero || BackwardDelta != Vector3.Zero || LeftDelta != Vector3.Zero || RightDelta != Vector3.Zero;
  // public CharacterDisplacementComponent Displace(Vector3 force) {
  //   if (Displacement == Vector3.Zero) {
  //     return this with { Displacement = force };
  //   }

  //   // Slerp between the current displacement and the new displacement
  //   // var newDisplacement = VectorUtils.Berp(Displacement, force, .5f, CurveUtils.SlerpXZ);
  //   // var newDisplacement = Vector3.Lerp(Displacement, force, .5f);

  //   var newDisplacement = Displacement + force;

  //   return this with { Displacement = newDisplacement };
  // }
}

public class CharacterSystem(ILogger<CharacterSystem> logger, EntityManager em, TimeManager tm, PhysxManager pm, PhysxCharacterManager pcm) : LarkSystem {
  public override Type[] RequiredComponents => [typeof(PhysxCharacterComponent), typeof(TransformComponent), typeof(CharacterMovementComponent)];

  public Action<(Guid, FrozenSet<ILarkComponent>), FrozenSet<ILarkInput>> MoveForward() => (entity, inputs) => {
    var (key, components) = entity;
    var (transform, displacement) = components.Get<TransformComponent, CharacterDisplacementComponent>();

    var vec = Vector3.Transform(-Vector3.UnitZ, transform.Rotation);
    var newDis = displacement with { ForwardDelta = vec };

    logger.LogInformation("MoveForward :: {displacement}", newDis);
    em.UpdateEntityComponent(key, newDis);
  };

  public Action<(Guid, FrozenSet<ILarkComponent>), FrozenSet<ILarkInput>> MoveBackward() => (entity, inputs) => {
    var (key, components) = entity;
    var (transform, displacement) = components.Get<TransformComponent, CharacterDisplacementComponent>();

    var vec = Vector3.Transform(Vector3.UnitZ, transform.Rotation);
    var newDis = displacement with { BackwardDelta = vec };

    logger.LogInformation("MoveBackward :: {displacement}", newDis);
    em.UpdateEntityComponent(key, newDis);
  };

  public Action<(Guid, FrozenSet<ILarkComponent>), FrozenSet<ILarkInput>> MoveLeft() => (entity, inputs) => {
    var (key, components) = entity;
    var (transform, displacement) = components.Get<TransformComponent, CharacterDisplacementComponent>();

    var vec = Vector3.Transform(-Vector3.UnitX, transform.Rotation);
    var newDis = displacement with { LeftDelta = vec };

    logger.LogInformation("MoveLeft :: {displacement}", newDis);
    em.UpdateEntityComponent(key, newDis);
  };

  public Action<(Guid, FrozenSet<ILarkComponent>), FrozenSet<ILarkInput>> MoveRight() => (entity, inputs) => {
    var (key, components) = entity;
    var (transform, displacement) = components.Get<TransformComponent, CharacterDisplacementComponent>();

    var vec = Vector3.Transform(Vector3.UnitX, transform.Rotation);
    var newDis = displacement with { RightDelta = vec };

    logger.LogInformation("MoveRight :: {displacement}", newDis);
    em.UpdateEntityComponent(key, newDis);
  };

  public override Task Init() {
    var cameraTransform = TransformComponent.Identity with {
      Position = new(40, -1, 0),
      Rotation = LarkUtils.CreateFromYawPitchRoll(0, 0, 0),
    };
    // new PhysxCapsuleComponent(.5f, 1f, false),
    em.AddEntity(
      new MetadataComponent("Player-1"),
      new CameraComponent() with { Active = true },
      cameraTransform,
      new CharacterDisplacementComponent(),
      new CharacterMovementComponent(.01f),
      new PhysxCharacterComponent(.5f, .95f),
      new ActionComponent("MoveForward", MoveForward()),
      new ActionComponent("MoveBackward", MoveBackward()),
      new ActionComponent("MoveLeft", MoveLeft()),
      new ActionComponent("MoveRight", MoveRight())
    );

    return Task.CompletedTask;
  }

  public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
    var (key, components) = Entity;

    var displacement = components.Get<CharacterDisplacementComponent>();
    if (displacement.Dirty) {
      var movement = components.Get<CharacterMovementComponent>();

      // Sum the displacements and normalize
      var displacementVector = Vector3.Normalize(displacement.ForwardDelta + displacement.BackwardDelta + displacement.LeftDelta + displacement.RightDelta);
      displacementVector *= movement.Speed * (float)tm.DeltaTime.TotalMilliseconds;

      logger.LogInformation("Moving player by {displacement}", displacementVector);
      pcm.Move(key, displacementVector, (float)tm.DeltaTime.TotalMilliseconds);
      em.UpdateEntityComponent(key, new CharacterDisplacementComponent());
    }
  }

  // TODO: This afterUpdate is being called before the update, which should not happen.
  // public override async void AfterUpdate() {
  //   await foreach (var (key, components) in em.GetEntitiesWithComponents([typeof(CharacterDisplacementComponent)])) {
  //     if (!components.Has<CharacterDisplacementComponent>()) {
  //       continue;
  //     }
  //     // clear the displacement component
  //     em.UpdateEntityComponent(key, new CharacterDisplacementComponent(Vector3.Zero));
  //   }
  // }
}

public class CharacterTransformSystem(PhysxCharacterManager pcm, EntityManager em) : LarkSystem {
  public override Type[] RequiredComponents => [typeof(PhysxCharacterComponent), typeof(CharacterDisplacementComponent), typeof(TransformComponent)];

  public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
    var (key, components) = Entity;
    if (!pcm.HasController(key)) {
      return;
    }

    var transform = components.Get<TransformComponent>();
    var position = pcm.GetPosition(key);
    em.UpdateEntityComponent(key, transform with { Position = position });
  }
}