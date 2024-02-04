
using System.Collections.Frozen;
using System.Diagnostics;
using System.Numerics;
using Lark.Engine;
using Lark.Engine.ecs;
using Lark.Engine.physx.systems;
using Lark.Engine.std;
using Lark.Game.components;
using Microsoft.Extensions.Logging;
using Silk.NET.GLFW;
using Silk.NET.Input;

namespace Lark.Game.systems;

public class InitSystem(EntityManager em, TimeManager tm, ActionManager am, InputManager im, ShutdownManager sm, CameraManager cm, LarkWindow window, ILogger<InitSystem> logger) : LarkSystem {
  public override Type[] RequiredComponents => [typeof(TransformComponent), typeof(CameraComponent)];

  public Stopwatch sw = new();

  public Action<(Guid, FrozenSet<ILarkComponent>), FrozenSet<ILarkInput>> KeyDown(LarkKeys key) {
    return (entity, events) => {
      var (key, components) = entity;

      if (LarkUtils.AnyKeyPressed(events)) {
        logger.LogInformation("KeyDown :: {key}", key);
      }

      if (LarkUtils.AnyKeyReleased(events)) {
        logger.LogInformation("KeyUp :: {key}", key);
      }

    };
  }

  public Action<(Guid, FrozenSet<ILarkComponent>), FrozenSet<ILarkInput>> Move(string name, float speed, Vector3 direction) {
    return (entity, events) => {
      logger.LogInformation("Move :: {name}", name);
      sw.Stop();
      var (key, components) = entity;
      var (velocity, transform) = components.Get<VelocityComponent, TransformComponent>();

      // If events doesn't have a pressed event for the key, return.
      // if (!LarkUtils.AnyKeyPressed(events)) return;

      // convert speed to m/s using delta time. A speed of 1 is 1m/s.
      var normalizedSpeed = speed / 10f * (float)tm.DeltaTime.TotalMilliseconds;

      // Calculate relative direction based on camera rotation.
      var relativeDirection = Vector3.Transform(direction, transform.Rotation);

      var moveDelta = velocity.MoveDelta + relativeDirection * normalizedSpeed;

      var newTransform = transform with {
        Position = direction * normalizedSpeed + transform.Position
      };

      // var newVelocity = velocity with {
      //   MoveDelta = moveDelta
      // };
      // logger.LogInformation("{name} :: {delta} :: {time}", name, newVelocity.MoveDelta, sw.ElapsedMilliseconds);

      em.UpdateEntityComponent(key, newTransform);
      sw.Restart();
    };
  }

  public Action<(Guid, FrozenSet<ILarkComponent>), FrozenSet<ILarkInput>> Jump(string name, TimeSpan jumpDuration) {
    return (entity, events) => {
      var (key, components) = entity;


      logger.LogInformation("Jump :: Triggered :: {duration} :: {key}", jumpDuration, key);

      var (transform, velocity) = components.Get<TransformComponent, VelocityComponent>();

      if (!components.Has<JumpComponent>()) {
        logger.LogInformation("Jump :: Add :: {duration} :: {key}", jumpDuration, key);
        em.AddEntityComponent(key, new JumpComponent(transform.Position, transform.Position, jumpDuration));
      }
    };
  }

  public Action<(Guid, FrozenSet<ILarkComponent>), FrozenSet<ILarkInput>> LookAt(float sensitivity = 1f) {
    var LastPos = Vector2.Zero;
    return (enttiy, events) => {

      if (!LarkUtils.AnyKeyPressed(events)) return;
      if (!LarkUtils.AnyEvent<ILarkCursorInput>(events, out var cursor)) return;

      if (cursor.Position == LastPos) return;
      LastPos = cursor.Position;
      var (key, components) = enttiy;
      var transform = components.Get<TransformComponent>();
      var worldPos = cm.ActiveCamera.ProjectToFar(cursor.Position) * sensitivity;
      logger.LogInformation("LookAt :: {position} :: {world}", cursor.Position, worldPos);

      var newTransform = transform with {
        Rotation = transform.Rotation * LarkUtils.LookAt(transform.Position, worldPos, Vector3.UnitY)
      };

      em.UpdateEntityComponent(key, newTransform);
      im.SetCursorPosition();
    };
  }

  public override Task Init() {
    // Disable cursor so it locks to the center of the window.
    window.SetCursorMode(CursorModeValue.CursorDisabled, true);
    var start = new TransformComponent(new(10, 0, 0), Vector3.One, Quaternion.Identity);

    // Add camera, position camera 15 units away from origin, 10 units above ground
    var cameraTransform = start with {
      Position = new(40, -10, 40),
      Rotation = LarkUtils.CreateFromYawPitchRoll(0, 0, 0),
    };

    am.AddActionToMap(ActionManager.DefaultMap, "MoveForward", new LarkKeyTrigger(LarkKeys.W));
    am.AddActionToMap(ActionManager.DefaultMap, "MoveBackward", new LarkKeyTrigger(LarkKeys.S));
    am.AddActionToMap(ActionManager.DefaultMap, "MoveLeft", new LarkKeyTrigger(LarkKeys.A));
    am.AddActionToMap(ActionManager.DefaultMap, "MoveRight", new LarkKeyTrigger(LarkKeys.D));
    am.AddActionToMap(ActionManager.DefaultMap, "LookAt", new LarkCursorTrigger());
    am.AddActionToMap(ActionManager.DefaultMap, "Exit", new LarkKeyTrigger(LarkKeys.Escape));
    am.AddActionToMap(ActionManager.DefaultMap, "Jump", new LarkKeyTrigger(LarkKeys.Space));

    var moveSpeed = 1f;

    var moveForwardAction = new ActionComponent("MoveForward", Move("MoveForward", moveSpeed, Vector3.UnitZ));
    var moveBackwardAction = new ActionComponent("MoveBackward", Move("MoveBackward", moveSpeed, -Vector3.UnitZ));
    var moveLeftAction = new ActionComponent("MoveLeft", Move("MoveLeft", moveSpeed, Vector3.UnitX));
    var moveRightAction = new ActionComponent("MoveRight", Move("MoveRight", moveSpeed, -Vector3.UnitX));

    var wAction = new ActionComponent("MoveForward", KeyDown(LarkKeys.W));
    var sAction = new ActionComponent("MoveBackward", KeyDown(LarkKeys.S));
    var aAction = new ActionComponent("MoveLeft", KeyDown(LarkKeys.A));
    var dAction = new ActionComponent("MoveRight", KeyDown(LarkKeys.D));

    var lookAtAction = new ActionComponent("LookAt", LookAt());
    var exitAction = new ActionComponent("Exit", (entity, input) => sm.Exit());
    var jumpAction = new ActionComponent("Jump", Jump("Jump", TimeSpan.FromSeconds(1.2f))); // 1.2 seconds


    // em.AddEntity(new MetadataComponent("Camera-1"), new PhysxCapsuleComponent(.05f, .5f, true), jumpAction, moveForwardAction, moveBackwardAction, moveLeftAction, moveRightAction, exitAction, new VelocityComponent(), new CameraComponent() with { Active = true }, cameraTransform);
    em.AddEntity(new MetadataComponent("Camera-1"), new VelocityComponent(), new CameraComponent() with { Active = true }, cameraTransform,
    exitAction, jumpAction,
    moveForwardAction, moveBackwardAction, moveLeftAction, moveRightAction
    // wAction, sAction, aAction, dAction
    );

    // em.AddEntity(new MeshComponent("antiqueCamera/AntiqueCamera.gltf"), new MetadataComponent("Antique-1"), start with { Position = new(15, 1, 0) });
    em.AddEntity(new MeshComponent("testPlane/test_plane.glb"), new MetadataComponent("plane-1"),
      start with { Position = new(0, 0, 0) });
    return Task.CompletedTask;
  }

  public override void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
    // var (key, components) = Entity;
    // var transform = components.Get<TransformComponent>();

    // // Create rotation looking at Vector3.UnitZ.

    // var newTransform = transform with {
    //   Position = new(40, -10, 40),
    //   Rotation = LarkUtils.CreateFromYawPitchRoll(0f, 0f, 0f),
    //   // Rotation = LarkUtils.CreateFromYawPitchRoll(0.0f, 0f, 90f),
    // };

    // // logger.LogInformation("Q :: {rot}", newTransform.Rotation);

    // em.UpdateEntityComponent(key, newTransform);

    // var camera = components.Get<CameraComponent>();
    // var newCamera = camera with {
    //   Active = true,
    //   Fov = 80,
    // };

    // em.UpdateEntityComponent(key, newCamera);
  }
}
