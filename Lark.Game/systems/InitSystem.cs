using System.Numerics;
using JoltPhysicsSharp;
using Lark.Engine.ecs;
using Lark.Engine.gi;
using Lark.Engine.jolt.components;
using Lark.Engine.jolt.managers;
using Lark.Engine.model;
using Lark.Engine.std;
using Microsoft.Extensions.Logging;

namespace Lark.Game.systems;

public class InitSystem(ILogger<InitSystem> logger, JoltManager jm, EntityManager em, ShutdownManager sm, ActionManager am) : LarkSystem, ILarkSystemInit {
  public override Type[] RequiredComponents => [];

  private static ILarkComponent[] GetCube(Vector3 pos, Vector3 scale, string name) => [
    new MeshComponent("cube/Cube.glb"),
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

    em.AddEntity(GetCube(new Vector3(40, 0, 20), new Vector3(1, -10, 1), "cube000"));
    em.AddEntity(GetCube(new Vector3(50, 0, 30), new Vector3(100, -10, 3), "cube001"));

    var pointLight = new PointLight(new LarkColor(1.0f, 1.0f, 1.0f), 1.0f, 10.0f);

    em.AddEntity(
      new MetadataComponent("Light001"),
      TransformComponent.Identity with { Position = new(0, -10, 0), Rotation = LarkUtils.CreateFromYawPitchRoll(0, 0, 0) },
      new LightComponent(new DirectionalLight(new LarkColor(1.0f, 1f, 1f, 1.0f), 10f))
    );
    em.AddEntity(
      new MetadataComponent("Light002"),
      TransformComponent.Identity with { Position = new(0, -2, 0) },
      new LightComponent(pointLight with { Color = new LarkColor(.0f, 1f, 1f, 1f), Intensity = 5.0f, Range = 15.0f })
    );
    // em.AddEntity(
    //   new MetadataComponent("Light003"),
    //   TransformComponent.Identity with { Position = new(0, -10, 10) },
    //   new LightComponent(pointLight with { Color = new LarkColor(.0f, 1.0f, .0f) })
    // );

    // light blue directional light
    // em.AddEntity(
    //   new MetadataComponent("Light004"),
    //   new LightComponent(new DirectionalLight(new LarkColor(0.0f, 0.0f, 1.0f), 1.0f))
    // );

    em.AddEntity([
      new MetadataComponent("fish001"),
      new MeshComponent("fish/BarramundiFish.gltf"),
      // new LarkSceneGraphComponent(playerId),
      start with {
        Position = new(0, -2f, 2),
        Scale = new(3, 3, 3),
        Rotation = LarkUtils.CreateFromYawPitchRoll(90, 180, 0),
       },
    ]);

    // em.AddEntity([
    //   new MetadataComponent("compare001"),
    //   new MeshComponent("compare/CompareRoughness.glb"),
    //   // new LarkSceneGraphComponent(playerId),
    //   start with {
    //     Position = new(0, -2f, -2),
    //     Scale = new(1f, 1f, 1f),
    //     Rotation = LarkUtils.CreateFromYawPitchRoll(0, 0, 0),
    //    },
    // ]);
    // em.AddEntity([
    //   new MetadataComponent("compare002"),
    //   new MeshComponent("compare/CompareMetallic.glb"),
    //   // new LarkSceneGraphComponent(playerId),
    //   start with {
    //     Position = new(0, -2f, 4),
    //     Scale = new(1, 1, 1),
    //     Rotation = LarkUtils.CreateFromYawPitchRoll(0, 0, 0),
    //    },
    // ]);
    // em.AddEntity([
    //   new MetadataComponent("compare003"),
    //   new MeshComponent("compare/CompareNormal.glb"),
    //   // new LarkSceneGraphComponent(playerId),
    //   start with {
    //     Position = new(0, -2f, -4),
    //     Scale = new(1, 1, 1),
    //     Rotation = LarkUtils.CreateFromYawPitchRoll(0, 0, 0),
    //    },
    // ]);



    em.AddEntity([
      new MetadataComponent("shoe001"),
      new MeshComponent("materialsVariantsShoe/MaterialsVariantsShoe.glb"),
      // new LarkSceneGraphComponent(playerId),
      start with {
        Position = new(0, -2f, -2),
        Scale = new(6, 6, 6),
        Rotation = LarkUtils.CreateFromYawPitchRoll(90, 180, 0),
       },
    ]);

    // em.AddEntity([
    //   new MetadataComponent("antique001"),
    //   new MeshComponent("boxTextured/BoxTextured.glb"),
    //   // new LarkSceneGraphComponent(playerId),
    //   start with {
    //     Position = new(0, -2f, 4),
    //     Scale = new(6, 6, 6),
    //     Rotation = LarkUtils.CreateFromYawPitchRoll(90, 180, 0),
    //    },
    // ]);

    // red spot light
    // em.AddEntity(
    //   new MetadataComponent("Light005"),
    //   TransformComponent.Identity with { Position = new(0, -10, 0) },
    //   new LightComponent(new SpotLight(new LarkColor(1.0f, 0.0f, 0.0f), 10.0f, 100.0f, 45.0f))
    // );

    am.AddActionToMap(ActionManager.DefaultMap, "Exit", new LarkKeyTrigger(LarkKeys.Escape));
    var exitAction = new ActionComponent("Exit", (entity, input) => sm.Exit());
    em.AddEntity(new MetadataComponent("Global-Actions"), exitAction);

    // var sw = new Stopwatch();
    // sw.Start();
    // logger.LogInformation("Initializing Octree...");
    // var octree = new LarkOctree();

    // octree.RegisterProbeGroup(new Vector3(100, 50, 100), 1f, Vector3.Zero);
    // octree.Build();

    // sw.Stop();
    // logger.LogInformation("Octree generated :: Probe {probeCount} :: {time}", octree.Probes.Length, sw.Elapsed);

    return Task.CompletedTask;
  }
}