using System.Collections.Frozen;
using System.Numerics;
using JoltPhysicsSharp;
using Lark.Engine;
using Lark.Engine.ecs;
using Lark.Engine.jolt.components;
using Lark.Engine.jolt.managers;
using Lark.Engine.jolt.systems;
using Lark.Engine.std;
using Lark.Engine.std.components;
using Lark.Engine.std.systems;
using Lark.Game.components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lark.Game.systems;


public class CharacterSystem(ILogger<CharacterSystem> logger, EntityManager em, TimeManager tm, ActionManager am, IOptions<GameSettings> gameSettings) : LarkSystem, ILarkSystemInit {
  public override Type[] RequiredComponents => [typeof(TransformComponent), typeof(JoltBodyInstanceComponent)];

  public Action<(Guid, FrozenSet<ILarkComponent>), FrozenSet<ILarkInput>> LookAt() {
    // One degree to radians
    const float toRadians = MathF.PI / 180;
    const float maxPitch = 90.0f * toRadians;
    const float minPitch = -89.0f * toRadians;

    return (entity, inputs) => {
      var (key, components) = entity;
      var character = components.Get<CharacterComponent>();
      var displacement = components.Get<CharacterRotationComponent>();
      var (targetId, targetComponents) = em.GetEntity(character.PlayerId);
      var targetTransform = targetComponents.Get<TransformComponent>();
      var cameraTransform = components.Get<TransformComponent>();

      // Get the current and last mouse positions from the inputs
      var currentMousePosition = inputs.OfType<ILarkCursorInput>().Last().Position;
      var lastMousePosition = displacement.LastMousePosition;

      // If the last mouse position is zero then set it to the current mouse position. This happens when the game starts.
      if (lastMousePosition == Vector2.Zero) {
        lastMousePosition = currentMousePosition;
      }

      // Calculate the delta mouse movement
      var deltaMouseMovement = (currentMousePosition - lastMousePosition) * gameSettings.Value.MouseSensitivity * toRadians;

      // Calculate the yaw and pitch rotations
      var yawRotation = Quaternion.CreateFromAxisAngle(-Vector3.UnitY, deltaMouseMovement.X);
      var pitchRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, deltaMouseMovement.Y);

      // Clamp the pitch rotation
      var totalPitch = Math.Clamp(displacement.TotalPitch - deltaMouseMovement.Y, minPitch, maxPitch) - displacement.TotalPitch;
      pitchRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, totalPitch);

      logger.LogInformation("LookAt :: {key} :: {pitchRotation} :: {yawRotation} :: {playerRotation} :: {cameraRotation}", key, pitchRotation, yawRotation, targetTransform.Rotation * pitchRotation, cameraTransform.Rotation * yawRotation);

      // Apply the yaw rotation to the player
      var newTargetTransform = targetTransform with { Rotation = targetTransform.Rotation * yawRotation };
      em.UpdateEntityComponent(targetId, newTargetTransform);

      // Apply the pitch rotation to the camera
      var newCameraTransform = cameraTransform with { Rotation = cameraTransform.Rotation * pitchRotation };
      em.UpdateEntityComponent(key, newCameraTransform);

      // Update the CharacterDisplacementComponent with the current mouse position
      var updatedDisplacement = displacement with { LastMousePosition = currentMousePosition, Rotation = newTargetTransform.Rotation, TotalPitch = totalPitch + displacement.TotalPitch };
      em.UpdateEntityComponent(key, updatedDisplacement);
    };
  }

  public Action<(Guid, FrozenSet<ILarkComponent>), FrozenSet<ILarkInput>> Move(Dictionary<LarkKeys, Vector3> directionLookup) {
    var LastFrame = 0;
    return (entity, inputs) => {
      var (key, components) = entity;
      if (LastFrame == tm.TotalFrames) {
        return;
      }
      LastFrame = tm.TotalFrames;

      // Find every key in inputs that is in the directionLookup. We will then add all the directions together. We will then normalize the vector along the unit circle so that the character moves at the same speed in all directions.
      var direction = inputs.OfType<ILarkKeyInput>().Where(k => directionLookup.ContainsKey(k.Key)).Select(k => directionLookup[k.Key]).Aggregate(Vector3.Zero, (acc, dir) => acc + dir);
      // Normalize direction to the unit circle so that the character moves at the same speed in all directions.
      direction = Vector3.Normalize(direction);

      var modifier = components.Get<CharacterDisplacementComponent>();
      var transform = components.Get<TransformComponent>();
      modifier.Set(dir => dir * direction);

      em.UpdateEntityComponent(key, modifier);

      logger.LogInformation("{frame} :: Move :: {key} :: {direction} :: {position}", tm.TotalFrames, key, direction, transform.Position);
    };
  }

  public static IEnumerable<ILarkComponent> BuildCharacter(Guid playerId, Guid bodyId, float speed = 5.0f) {
    yield return new CharacterComponent(speed, playerId, bodyId);
    yield return new CharacterRotationComponent() { Rotation = LarkUtils.CreateFromYawPitchRoll(90, 0, 0) };
    yield return new CharacterDisplacementComponent();
  }

  public IEnumerable<ILarkComponent> BuildActions() {
    yield return new ActionComponent("LookAt", LookAt());
    // Todo: this assumes we are using wasd for movement. We should make this configurable.
    yield return new ActionComponent("Move", Move(
      new Dictionary<LarkKeys, Vector3> {
        { LarkKeys.W, Vector3.UnitZ },
        { LarkKeys.S, -Vector3.UnitZ },
        { LarkKeys.A, Vector3.UnitX },
        { LarkKeys.D, -Vector3.UnitX }
      }
    ));
  }

  public Task Init() {
    am.AddActionToMap(ActionManager.DefaultMap, "LookAt", new LarkCursorTrigger());
    // Todo: this assumes we are using wasd for movement. We should make this configurable.
    am.AddActionToMap(ActionManager.DefaultMap, "Move", new LarkMultiTrigger([
      new LarkKeyTrigger(LarkKeys.W),
      new LarkKeyTrigger(LarkKeys.S),
      new LarkKeyTrigger(LarkKeys.A),
      new LarkKeyTrigger(LarkKeys.D),
    ]));

    // Entity: Player (root entity, characterVirtual)
    // -> Entity: Player::body (physics body)
    // -> Entity: Player::camera (camera)

    var playerTransform = TransformComponent.Identity with {
      Position = new(0, -2.6f, 0),
      Rotation = LarkUtils.CreateFromYawPitchRoll(-90, 0, 0),
    };
    var playerId = em.AddEntity(
      new MetadataComponent("Player-1"),
      // new MeshComponent("capsule/capsule.glb"),
      playerTransform,
      new JoltCharacterComponent() {
        Shape = new CapsuleShape(.5f, 1.0f),
      }
    );

    var meshId = em.AddEntity([
      new MetadataComponent("Player-1::mesh"),
      new MeshComponent("damagedHelmet/DamagedHelmet.glb"),
      // new LarkSceneGraphComponent(playerId),
      playerTransform with {
        Position = new(0, -2, 0),
        Rotation = LarkUtils.CreateFromYawPitchRoll(0, 180, 90),
       },
    ]);

    var bodyId = em.AddEntity([
      new MetadataComponent("Player-1::body"),
      new LarkIgnoreWarningComponent(JoltConstraintSystem.MissingConstraintError, "Because we use a virtual character we don't have a BodyID."),
      TransformComponent.Identity,
      new LarkSceneGraphComponent(playerId),
      new JoltBodyComponent() {
        Shape = new CapsuleShapeSettings(.45f, .95f),
        MotionType = MotionType.Kinematic,
        ObjectLayer = Layers.Character
      },
    ]);

    em.AddEntity([new MetadataComponent("Player-1::camera"),
      TransformComponent.Identity,
      // new LarkSceneGraphComponent(playerId),
      new CameraComponent() with { Active = true },
      new JoltCameraTransformComponent() {
        Position = new(-2, 0, 0),
        Rotation = LarkUtils.CreateFromYawPitchRoll(-180, 0, 0)
      },
      new JoltBodyComponent() {
        Shape = new SphereShapeSettings(.1f),
        MotionType = MotionType.Kinematic,
        ObjectLayer = Layers.Character
      },
      // new ActiveAbilitySet("Hero1"),
      ..BuildActions(),
      ..BuildCharacter(playerId, bodyId)
    ]);

    logger.LogInformation("Player Created");

    return Task.CompletedTask;
  }

}

public record struct JoltCameraTransformComponent : ILarkComponent {
  public Vector3 Position { get; init; }
  public Quaternion Rotation { get; init; }
}
