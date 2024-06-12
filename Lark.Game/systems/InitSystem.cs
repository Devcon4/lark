using System.Collections.Frozen;
using System.Numerics;
using JoltPhysicsSharp;
using Lark.Engine.ecs;
using Lark.Engine.jolt.components;
using Lark.Engine.jolt.managers;
using Lark.Engine.std;
using Microsoft.Extensions.Logging;

namespace Lark.Game.systems;

public class InitSystem(ILogger<InitSystem> logger, JoltManager jm, EntityManager em, ShutdownManager sm, ActionManager am) : LarkSystem, ILarkSystemInit {
  public override Type[] RequiredComponents => [];

  private static ILarkComponent[] GetCube(Vector3 pos, Vector3 scale, string name) => [
    new MeshComponent("cube/cube.glb"),
    new MetadataComponent(name),
    new JoltBodyComponent {
      Shape = new BoxShapeSettings(new Vector3(1.0f, 1.0f, 1.0f)),
      MotionType = MotionType.Static,
      ObjectLayer = Layers.NonMoving
    },
    TransformComponent.Identity with { Position = pos, Scale = scale, Rotation = LarkUtils.CreateFromYawPitchRoll(0, 0, 0) }
  ];

  public Task Init() {
    logger.LogInformation("Initializing Jolt Physics");
    var start = new TransformComponent(new(10, 0, 0), Vector3.One, LarkUtils.CreateFromYawPitchRoll(0, 0, 0));

    jm.CreateSystem();

    em.AddEntity(
      new MetadataComponent("plane-1"),
      new MeshComponent("testPlane/test_plane_old.glb"),
      start with { Position = new(0, 0, 0), Rotation = LarkUtils.CreateFromYawPitchRoll(0, -180, 0) },
      new JoltBodyComponent {
        Shape = new BoxShapeSettings(new Vector3(100f, 1.0f, 100f)),
        MotionType = MotionType.Static,
        ObjectLayer = Layers.NonMoving
      }
      );

    em.AddEntity(GetCube(new Vector3(40, 0, 20), new Vector3(1, 10, 1), "cube000"));
    em.AddEntity(GetCube(new Vector3(50, 0, 30), new Vector3(100, 10, 3), "cube001"));

    am.AddActionToMap(ActionManager.DefaultMap, "Exit", new LarkKeyTrigger(LarkKeys.Escape));
    var exitAction = new ActionComponent("Exit", (entity, input) => sm.Exit());
    em.AddEntity(new MetadataComponent("Global-Actions"), exitAction);


    return Task.CompletedTask;
  }
}