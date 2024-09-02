using System.Numerics;
using JoltPhysicsSharp;
using Lark.Engine.ecs;
using Lark.Engine.jolt.components;
using Lark.Engine.jolt.managers;
using Lark.Engine.std;
using Lark.Game.components;
using Microsoft.Extensions.Logging;

namespace Lark.Game.systems;

public class CharacterDisplacementSystem(EntityManager em, JoltManager jm, TimeManager tm) : LarkSystem, ILarkSystemBeforeUpdate, ILarkSystemAfterUpdate {
  public override Type[] RequiredComponents => [typeof(CharacterRotationComponent), typeof(CharacterDisplacementComponent), typeof(CharacterComponent)];

  public void BeforeUpdate() {
    foreach (var entity in em.GetEntitiesWithComponentsSync(RequiredComponents)) {
      // Clear the modifiers
      var (key, components) = entity;
      var characterDisplacement = components.Get<CharacterDisplacementComponent>();
      characterDisplacement = characterDisplacement with { Modifiers = [] };
      em.UpdateEntityComponent(key, characterDisplacement);
    }
  }

  public void AfterUpdate() {
    foreach (var (key, components) in em.GetEntitiesWithComponentsSync(RequiredComponents)) {
      var character = components.Get<CharacterComponent>();
      var (targetKey, targetComponents) = em.GetEntity(character.PlayerId);

      var transform = targetComponents.Get<TransformComponent>();
      var characterDisplacement = components.Get<CharacterDisplacementComponent>();
      var characterRotation = components.Get<CharacterRotationComponent>();

      var displacementVector = Vector3.One;

      foreach (var modifier in characterDisplacement.Modifiers) {
        displacementVector = modifier(displacementVector);
      }

      if (characterDisplacement.Modifiers.Count == 0) {
        displacementVector = Vector3.Zero;
      }

      // If any part of displacementVector is NaN, set it to zero
      if (float.IsNaN(displacementVector.X) || float.IsNaN(displacementVector.Y) || float.IsNaN(displacementVector.Z)) {
        displacementVector = Vector3.Zero;
      }

      displacementVector.Y = -displacementVector.Y;

      // displacementVector is relative to the character's forward direction
      displacementVector = Vector3.Transform(displacementVector, characterRotation.Rotation);

      // DisplacementVector is an idealized velocity of the character. For example if the character is moving forward, the displacement vector is (0, 0, 1).
      // This corresponds to the character moving forward at a speed of 1 meter per second. We need to normalize this vector by time to get the actual velocity.
      displacementVector *= character.Speed;

      var (characterId, systemId) = targetComponents.Get<JoltCharacterInstance>();
      var bi = jm.GetBodyInterface(systemId);
      var c = jm.GetCharacter(characterId);

      // Apply gravity.
      if (c.GroundState == GroundState.InAir) {
        // We hardcode gravity here. This should prob be configurable. Jolt is +Y up so we negate the gravity.
        displacementVector += new Vector3(0, -9.8f, 0) * -Vector3.UnitY;
      }

      var rotation = characterRotation.Rotation;
      rotation.Y = -rotation.Y;
      rotation.W = -rotation.W;

      c.Rotation = rotation;
      // c.Position += displacementVector;
      c.LinearVelocity = displacementVector;

      // Global death plane; reset the player to the origin.
      if (transform.Position.Y < -10000) {
        c.Position = new Vector3(0, -3, 0);
      }

      // A player should have a child body which is the actual physics body. This is so we can do raycasts and other physics operations on the player.
      var (bodyKey, bodyComponents) = em.GetEntity(character.BodyId);
      var bodyInstance = bodyComponents.Get<JoltBodyInstanceComponent>();
      var bodyTransform = bodyComponents.Get<TransformComponent>();
      jm.SimulateCharacter(characterId, systemId);
      c = jm.GetCharacter(characterId);

      var bodyPos = c.Position;
      bodyPos.Y = -bodyPos.Y;
      bi.MoveKinematic(bodyInstance.BodyId, bodyPos, c.Rotation, (float)tm.DeltaTime.TotalSeconds);

      // var cameraTransform = components.Get<TransformComponent>();

      // var cameraBodyInstance = components.Get<JoltBodyInstanceComponent>();

      // bi.MoveKinematic(cameraBodyInstance.BodyId, bodyPos + new Vector3(-2, 0, 0), c.Rotation * LarkUtils.CreateFromYawPitchRoll(-180, 0, 0), (float)tm.DeltaTime.TotalSeconds);
      // var cameraBody = jm.GetBodyRead(cameraBodyInstance.SystemId, cameraBodyInstance.BodyId);
      // var bodyBody = jm.GetBodyRead(bodyInstance.SystemId, bodyInstance.BodyId);

      // logger.LogInformation("FollowingBodies :: {key} :: {cameraBody} :: {cameraTransform}", key, cameraBody.Instance.Position, bodyPos);

      var cameraInstance = components.Get<JoltBodyInstanceComponent>();
      var cameraTransform = components.Get<JoltCameraTransformComponent>();

      var cameraPos = cameraTransform.Position;
      cameraPos.Y = -cameraPos.Y;

      cameraPos += bodyPos;
      bi.MoveKinematic(cameraInstance.BodyId, cameraPos, c.Rotation * cameraTransform.Rotation, (float)tm.DeltaTime.TotalSeconds);
      // bi.SetPosition(cameraInstance.BodyId, cameraPos, Activation.Activate);
      // bi.SetRotation(cameraInstance.BodyId, c.Rotation * cameraTransform.Rotation, Activation.Activate);
    }
  }
}