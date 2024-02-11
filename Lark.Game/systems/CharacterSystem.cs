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
public record struct CharacterDisplacementComponent(Vector3 ForwardDelta, Vector3 BackwardDelta, Vector3 LeftDelta, Vector3 RightDelta, Vector3 JumpDelta) : ILarkComponent {
  public readonly bool Dirty =>
    ForwardDelta != Vector3.Zero ||
    BackwardDelta != Vector3.Zero ||
    LeftDelta != Vector3.Zero ||
    RightDelta != Vector3.Zero ||
    JumpDelta != Vector3.Zero;
}

public class CharacterSystem(ILogger<CharacterSystem> logger, EntityManager em, TimeManager tm, PhysxManager pm, PhysxCharacterManager pcm) : LarkSystem {
  public override Type[] RequiredComponents => [typeof(PhysxCharacterComponent), typeof(TransformComponent), typeof(CharacterMovementComponent)];

  public Action<(Guid, FrozenSet<ILarkComponent>), FrozenSet<ILarkInput>> MoveForward() => (entity, inputs) => {
    var (key, components) = entity;
    var (transform, displacement) = components.Get<TransformComponent, CharacterDisplacementComponent>();

    var vec = Vector3.Transform(-Vector3.UnitZ, transform.Rotation);
    var newDis = displacement with { ForwardDelta = vec };
    em.UpdateEntityComponent(key, newDis);
  };

  public Action<(Guid, FrozenSet<ILarkComponent>), FrozenSet<ILarkInput>> MoveBackward() => (entity, inputs) => {
    var (key, components) = entity;
    var (transform, displacement) = components.Get<TransformComponent, CharacterDisplacementComponent>();

    var vec = Vector3.Transform(Vector3.UnitZ, transform.Rotation);
    var newDis = displacement with { BackwardDelta = vec };

    em.UpdateEntityComponent(key, newDis);
  };

  public Action<(Guid, FrozenSet<ILarkComponent>), FrozenSet<ILarkInput>> MoveLeft() => (entity, inputs) => {
    var (key, components) = entity;
    var (transform, displacement) = components.Get<TransformComponent, CharacterDisplacementComponent>();

    var vec = Vector3.Transform(-Vector3.UnitX, transform.Rotation);
    var newDis = displacement with { LeftDelta = vec };

    em.UpdateEntityComponent(key, newDis);
  };

  public Action<(Guid, FrozenSet<ILarkComponent>), FrozenSet<ILarkInput>> MoveRight() => (entity, inputs) => {
    var (key, components) = entity;
    var (transform, displacement) = components.Get<TransformComponent, CharacterDisplacementComponent>();

    var vec = Vector3.Transform(Vector3.UnitX, transform.Rotation);
    var newDis = displacement with { RightDelta = vec };

    em.UpdateEntityComponent(key, newDis);
  };

  public Action<(Guid, FrozenSet<ILarkComponent>), FrozenSet<ILarkInput>> Jump() {
    return (entity, inputs) => {
      var (key, components) = entity;
      logger.LogInformation("Jump :: Begin");
      // var (transform, displacement) = components.Get<TransformComponent, CharacterDisplacementComponent>();

      // If we have a jump component then we are already jumping
      if (components.Has<CharacterJumpComponent>()) {
        return;
      }

      logger.LogInformation("Jump :: Adding jump component");
      var transform = components.Get<TransformComponent>();
      var jump = new CharacterJumpComponent(CurveUtils.Jump, TimeSpan.FromSeconds(2), 1f, transform.Position);
      em.AddEntityComponent(key, jump);
    };
  }

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
      new ActionComponent("MoveRight", MoveRight()),
      new ActionComponent("Jump", Jump())
    );

    return Task.CompletedTask;
  }

  public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
    var (key, components) = Entity;

    var displacement = components.Get<CharacterDisplacementComponent>();
    if (displacement.Dirty) {
      var movement = components.Get<CharacterMovementComponent>();

      // Sum the movement displacements and normalize.
      var displacementVector = Vector3.Normalize(displacement.ForwardDelta + displacement.BackwardDelta + displacement.LeftDelta + displacement.RightDelta);
      // if displacementVector is nan, set it to zero

      if (float.IsNaN(displacementVector.X) || float.IsNaN(displacementVector.Y) || float.IsNaN(displacementVector.Z)) {
        displacementVector = Vector3.Zero;
      }

      displacementVector *= movement.Speed * (float)tm.DeltaTime.TotalMilliseconds;

      logger.LogInformation("CharacterSystem :: Update :: {key} :: displacement {displacementVector} :: jump {jump}", key, displacementVector, displacement.JumpDelta);

      // Dont normalize the jump delta.
      displacementVector += displacement.JumpDelta;

      pcm.Move(key, displacementVector, (float)tm.DeltaTime.TotalMilliseconds);
      em.UpdateEntityComponent(key, new CharacterDisplacementComponent());
    }
  }
}
