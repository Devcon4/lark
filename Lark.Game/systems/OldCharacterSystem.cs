using System.Collections.Frozen;
using System.Numerics;
using Lark.Engine;
using Lark.Engine.ecs;

using Lark.Engine.std;
using Lark.Engine.std.systems;
using Lark.Game.managers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;
using Silk.NET.Maths;

namespace Lark.Game.systems;

// public record struct CharacterDisplacementComponent(Vector3 ForwardDelta, Vector3 BackwardDelta, Vector3 LeftDelta, Vector3 RightDelta, Vector3 JumpDelta) : ILarkComponent {
//   public readonly bool Dirty =>
//     ForwardDelta != Vector3.Zero ||
//     BackwardDelta != Vector3.Zero ||
//     LeftDelta != Vector3.Zero ||
//     RightDelta != Vector3.Zero ||
//     JumpDelta != Vector3.Zero;
// // }

// public class CharacterSystem(ILogger<CharacterSystem> logger, EntityManager em, TimeManager tm, ActionManager am, AbilitySetManager asm, PhysxCharacterManager pcm, IOptions<GameSettings> gameSettings) : LarkSystem, ILarkSystemInit {
//   public override Type[] RequiredComponents => [typeof(CharacterComponent)];

//   // public Action<(Guid, FrozenSet<ILarkComponent>), FrozenSet<ILarkInput>> Move() => (entity, inputs) => {
//   //   var (key, components) = entity;
//   //   var (transform, displacement) = components.Get<TransformComponent, CharacterDisplacementComponent>();

//   //   var newDis = displacement with { };

//   //   em.UpdateEntityComponent(key, newDis);
//   // };

//   public Action<(Guid, FrozenSet<ILarkComponent>), FrozenSet<ILarkInput>> MoveForward() => (entity, inputs) => {
//     var (key, components) = entity;
//     var (transform, displacement) = components.Get<TransformComponent, CharacterDisplacementComponent>();

//     var newDis = displacement with { ForwardDelta = -Vector3.UnitZ };

//     em.UpdateEntityComponent(key, newDis);
//   };

//   public Action<(Guid, FrozenSet<ILarkComponent>), FrozenSet<ILarkInput>> MoveBackward() => (entity, inputs) => {
//     var (key, components) = entity;
//     var (transform, displacement) = components.Get<TransformComponent, CharacterDisplacementComponent>();

//     var newDis = displacement with { BackwardDelta = Vector3.UnitZ };

//     em.UpdateEntityComponent(key, newDis);
//   };

//   public Action<(Guid, FrozenSet<ILarkComponent>), FrozenSet<ILarkInput>> MoveLeft() => (entity, inputs) => {
//     var (key, components) = entity;
//     var (transform, displacement) = components.Get<TransformComponent, CharacterDisplacementComponent>();

//     var newDis = displacement with { LeftDelta = -Vector3.UnitX };

//     em.UpdateEntityComponent(key, newDis);
//   };

//   public Action<(Guid, FrozenSet<ILarkComponent>), FrozenSet<ILarkInput>> MoveRight() => (entity, inputs) => {
//     var (key, components) = entity;
//     var (transform, displacement) = components.Get<TransformComponent, CharacterDisplacementComponent>();

//     var newDis = displacement with { RightDelta = Vector3.UnitX };

//     em.UpdateEntityComponent(key, newDis);
//   };


//   public Action<(Guid, FrozenSet<ILarkComponent>), FrozenSet<ILarkInput>> UseAbility(string abilityName) {
//     return (entity, events) => {
//       var (key, components) = entity;
//       var trigger = components.GetList<TriggerAbility>();

//       var hasTrigger = trigger.Any(t => t.AbilityName == abilityName);

//       // If we have a trigger and the mouse is released then remove the trigger.
//       if (hasTrigger && LarkUtils.AnyMouseReleased(events)) {
//         logger.LogInformation("{abilityName} :: Released :: {frame}", abilityName, tm.TotalFrames);
//         em.RemoveEntityComponent(key, t => t is TriggerAbility ta && ta.AbilityName == abilityName);
//         return;
//       }

//       // If we have a trigger then we are already attacking.
//       if (hasTrigger) {
//         return;
//       }

//       // Trigger the ability if the mouse is pressed.
//       if (LarkUtils.AnyMousePressed(events)) {
//         logger.LogInformation("{abilityName} :: pressed :: {frame}", abilityName, tm.TotalFrames);
//         em.AddEntityComponent(key, new TriggerAbility(abilityName));
//       }
//     };
//   }

//   public Action<(Guid, FrozenSet<ILarkComponent>), FrozenSet<ILarkInput>> AltAttack() => UseAbility("AltAttack");
//   public Action<(Guid, FrozenSet<ILarkComponent>), FrozenSet<ILarkInput>> MainAttack() => UseAbility("MainAttack");

//   public Action<(Guid, FrozenSet<ILarkComponent>), FrozenSet<ILarkInput>> Jump() {
//     return (entity, inputs) => {
//       var (key, components) = entity;
//       logger.LogInformation("Jump :: Begin");
//       // var (transform, displacement) = components.Get<TransformComponent, CharacterDisplacementComponent>();

//       // If we have a jump component then we are already jumping
//       if (components.Has<CharacterJumpComponent>()) {
//         return;
//       }

//       logger.LogInformation("Jump :: Adding jump component");
//       var transform = components.Get<TransformComponent>();
//       var jump = new CharacterJumpComponent(CurveUtils.Jump, TimeSpan.FromSeconds(2), 1f, transform.Position);
//       em.AddEntityComponent(key, jump);
//     };
//   }

//   public Action<(Guid, FrozenSet<ILarkComponent>), FrozenSet<ILarkInput>> LookAt() {
//     // One degree to radians
//     const float toRadians = MathF.PI / 180;
//     const float maxPitch = 90.0f * toRadians;
//     const float minPitch = -89.0f * toRadians;

//     return (entity, inputs) => {
//       var (key, components) = entity;
//       var displacement = components.Get<CharacterRotationComponent>();
//       var transform = components.Get<TransformComponent>();

//       // Sometimes the rotation is zero, so we need to set it to identity.
//       if (transform.Rotation == Quaternion.Zero) {
//         transform = transform with { Rotation = Quaternion.Identity };
//       }

//       // Get the current mouse position from the inputs
//       var currentMousePosition = inputs.OfType<ILarkCursorInput>().Last().Position;
//       // Get the last mouse position
//       var lastMousePosition = displacement.LastMousePosition;

//       // If the last mouse position is zero then set it to the current mouse position. This happens when the game starts.
//       if (lastMousePosition == Vector2.Zero) {
//         lastMousePosition = currentMousePosition;
//       }

//       // sensitivity is in degrees. deltaMouse is in radians. So we need to convert the sensitivity to radians.
//       var fullSense = gameSettings.Value.MouseSensitivity * toRadians;

//       // Calculate the delta mouse movement
//       var deltaMouseMovement = (currentMousePosition - lastMousePosition) * fullSense;

//       var testPitch = displacement.TotalPitch - deltaMouseMovement.Y;
//       var newPitch = Math.Clamp(testPitch, minPitch, maxPitch) - displacement.TotalPitch;


//       var yawRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, deltaMouseMovement.X);
//       var pitchRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, newPitch);

//       var newRotation = pitchRotation * transform.Rotation * yawRotation;

//       logger.LogInformation("Transform :: {key} :: {newRotation} :: {TotalPitch} :: {NewPitch}", key, newRotation, displacement.TotalPitch, newPitch);

//       // Update the CharacterDisplacementComponent with the current mouse position
//       var updatedDisplacement = displacement with { LastMousePosition = currentMousePosition, Rotation = newRotation, TotalPitch = newPitch + displacement.TotalPitch };
//       em.UpdateEntityComponent(key, updatedDisplacement);
//     };
//   }

//   public Task Init() {

//     asm.CreateAbilitySet("Hero1");

//     asm.AddAbilityToSet("Hero1", "MainAttack", new HeroMainAttack(85f, .5f, 1f, 100f, Duration.FromMilliseconds(200), CurveUtils.Linear));
//     asm.AddAbilityToSet("Hero1", "AltAttack", new HeroAltAttack(85f, .5f, 20f, Duration.FromSeconds(3), Duration.FromSeconds(4), CurveUtils.LinearForward, CurveUtils.LinearForward));

//     am.AddActionToMap(ActionManager.DefaultMap, "MainAttack", new LarkMouseTrigger(LarkMouseButton.Left));
//     am.AddActionToMap(ActionManager.DefaultMap, "AltAttack", new LarkMouseTrigger(LarkMouseButton.Right));
//     am.AddActionToMap(ActionManager.DefaultMap, "LookAt", new LarkCursorTrigger());

//     var playerTransform = TransformComponent.Identity with {
//       Position = new(0, -1, 0),
//       Rotation = LarkUtils.CreateFromYawPitchRoll(0, 0, 0),
//     };
//     var playerId = em.AddEntity(
//       new MetadataComponent("Player-1"),
//       playerTransform,
//       new PhysxCharacterComponent(.5f, .95f)
//     );

//     em.AddEntity([new MetadataComponent("Player-1::camera"),
//       playerTransform with {
//         Position = new(-2, -1, 0),
//         Rotation = LarkUtils.CreateFromYawPitchRoll(0, 0, 0)
//       },
//       new LarkSceneGraphComponent(playerId),
//       new CameraComponent() with { Active = true },
//       new ActiveAbilitySet("Hero1"),
//       ..BuildActions(),
//       ..BuildCharacter(playerId)
//     ]);

//     return Task.CompletedTask;
//   }

//   public IEnumerable<ILarkComponent> BuildActions() {
//     // yield return new ActionComponent("Jump", Jump());
//     yield return new ActionComponent("MainAttack", MainAttack());
//     yield return new ActionComponent("AltAttack", AltAttack());
//     yield return new ActionComponent("LookAt", LookAt());
//     yield return new ActionComponent("MoveForward", MoveForward());
//     yield return new ActionComponent("MoveBackward", MoveBackward());
//     yield return new ActionComponent("MoveLeft", MoveLeft());
//     yield return new ActionComponent("MoveRight", MoveRight());
//   }

//   public IEnumerable<ILarkComponent> BuildCharacter(Guid targetId, float speed = 0.01f) {
//     yield return new CharacterComponent(speed, targetId);
//     yield return new CharacterRotationComponent();
//     yield return new CharacterDisplacementComponent();
//   }

//   public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
//     var (key, components) = Entity;

//     var displacement = components.Get<CharacterDisplacementComponent>();
//     if (displacement.Dirty) {
//       var charBase = components.Get<CharacterComponent>();

//       // Sum the charBase displacements and normalize.
//       var displacementVector = Vector3.Normalize(displacement.ForwardDelta + displacement.BackwardDelta + displacement.LeftDelta + displacement.RightDelta);
//       // if displacementVector is nan, set it to zero

//       if (float.IsNaN(displacementVector.X) || float.IsNaN(displacementVector.Y) || float.IsNaN(displacementVector.Z)) {
//         displacementVector = Vector3.Zero;
//       }

//       displacementVector *= charBase.Speed * (float)tm.DeltaTime.TotalMilliseconds;

//       // Dont normalize the jump delta.
//       displacementVector += displacement.JumpDelta;

//       // displacementVector is relative to forward, so we need to transform it to world space.
//       var transform = components.Get<TransformComponent>();
//       displacementVector = Vector3.Transform(displacementVector, transform.Rotation);

//       pcm.Move(charBase.TargetId, displacementVector, (float)tm.DeltaTime.TotalMilliseconds);
//       em.UpdateEntityComponent(key, new CharacterDisplacementComponent());
//     }
//   }
// }
