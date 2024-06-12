using System.Numerics;
using JoltPhysicsSharp;
using Lark.Engine.ecs;
using Lark.Engine.std;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Silk.NET.Vulkan;

namespace Lark.Engine.jolt.managers;

public static class Layers {
  public static readonly ObjectLayer NonMoving = 0;
  public static readonly ObjectLayer Moving = 1;
  public static readonly ObjectLayer Character = 2;
}

public static class BroadPhaseLayers {
  public static readonly BroadPhaseLayer NonMoving = 0;
  public static readonly BroadPhaseLayer Moving = 1;
  public static readonly BroadPhaseLayer Character = 2;
}

public record CharacterCreateInfo(Guid SystemId, CapsuleShape Shape, Vector3 Position, Quaternion Rotation, float MaxSlopeAngle = 80.0f, float Mass = 75.0f);


public class JoltManager(ILogger<JoltManager> logger, TimeManager tm) : LarkManager {
  public const string DefaultSystem = "Default";
  private Dictionary<Guid, PhysicsSystem> Systems { get; } = [];
  private Dictionary<Guid, CharacterVirtual> Characters { get; } = [];
  private Dictionary<Guid, Constraint> Constraints { get; } = [];
  public Dictionary<string, Guid> SystemLookup { get; } = [];

  private Dictionary<Guid, HashSet<Guid>> SystemToCharacters { get; } = [];

  public override Task Init() {
    Foundation.Init(0u, false);

    return base.Init();
  }

  public override Task Cleanup() {

    foreach (var system in Systems) {
      system.Value.Dispose();
    }

    Foundation.Shutdown();
    return base.Cleanup();
  }

  public void SetGravity(Guid systemId, Vector3 gravity) {
    if (!Systems.ContainsKey(systemId)) {
      logger.LogError("Jolt :: System not found");
      return;
    }

    Systems[systemId].Gravity = gravity;
  }

  public Guid CreateSystem(string? name = null) {
    logger.LogInformation("Creating Jolt System");

    name ??= DefaultSystem;

    if (SystemLookup.ContainsKey(name)) {
      throw new Exception("Jolt :: System with name already exists");
    }

    // Setup 2 default layers. TODO: Make this configurable
    ObjectLayerPairFilterTable objectLayerTable = new(6);
    objectLayerTable.EnableCollision(Layers.NonMoving, Layers.Moving);
    objectLayerTable.EnableCollision(Layers.NonMoving, Layers.Character);
    objectLayerTable.EnableCollision(Layers.Moving, Layers.Moving);
    objectLayerTable.EnableCollision(Layers.Moving, Layers.Character);
    objectLayerTable.EnableCollision(Layers.Character, Layers.Moving);
    objectLayerTable.EnableCollision(Layers.Character, Layers.NonMoving);

    BroadPhaseLayerInterfaceTable broadPhaseLayerTable = new(3, 6);
    broadPhaseLayerTable.MapObjectToBroadPhaseLayer(Layers.NonMoving, BroadPhaseLayers.NonMoving);
    broadPhaseLayerTable.MapObjectToBroadPhaseLayer(Layers.Moving, BroadPhaseLayers.Moving);
    broadPhaseLayerTable.MapObjectToBroadPhaseLayer(Layers.Character, BroadPhaseLayers.Character);
    var objectVsBroadPhaseLayerTable = new ObjectVsBroadPhaseLayerFilterTable(broadPhaseLayerTable, 3, objectLayerTable, 6);


    PhysicsSystemSettings settings = new() {
      ObjectLayerPairFilter = objectLayerTable,
      BroadPhaseLayerInterface = broadPhaseLayerTable,
      ObjectVsBroadPhaseLayerFilter = objectVsBroadPhaseLayerTable,
      MaxBodies = 1024,
      MaxBodyPairs = 1024,
      MaxContactConstraints = 1024,
    };

    var systemId = Guid.NewGuid();
    var system = new PhysicsSystem(settings) {
      // Default gravity to 9.8 m/s^2 in the negative Y direction
      Gravity = new Vector3(0, -9.8f, 0)
    };

    Systems.Add(systemId, system);
    SystemLookup.Add(name, systemId);
    return systemId;
  }

  public Guid GetSystemId(string name) {
    if (!SystemLookup.TryGetValue(name, out var systemId)) {
      throw new Exception("Jolt :: System not found");
    }

    return systemId;
  }

  public Guid AddConstraint(BodyID parentId, BodyID childID, TwoBodyConstraintSettings settings, Guid? systemId) {

    systemId ??= GetSystemId(DefaultSystem);

    if (systemId is null) {
      throw new Exception("Jolt :: System not found");
    }

    var parent = GetBodyRead(systemId.Value, parentId);
    var child = GetBodyRead(systemId.Value, childID);

    if (parent is null) {
      throw new Exception("Jolt :: Parent body not found");
    }

    if (child is null) {
      throw new Exception("Jolt :: Child body not found");
    }

    var constraint = settings.CreateConstraint(parent.Instance, child.Instance);
    var constraintId = Guid.NewGuid();

    Constraints.Add(constraintId, constraint);
    return constraintId;
  }

  public BodyInterface GetBodyInterface(Guid systemId) {
    return Systems[systemId].BodyInterface;
  }

  public LarkBodyRead GetBodyRead(Guid systemId, BodyID bodyId) {
    if (!Systems.TryGetValue(systemId, out var system)) {
      throw new Exception("Jolt :: System not found");
    }

    return new LarkBodyRead(system.BodyLockInterface, bodyId);
  }

  public LarkBodyWrite GetBodyWrite(Guid systemId, BodyID bodyId) {
    if (!Systems.TryGetValue(systemId, out var system)) {
      throw new Exception("Jolt :: System not found");
    }

    return new LarkBodyWrite(system.BodyLockInterface, bodyId);
  }

  public Guid CreateCharacter(CharacterCreateInfo create) {
    if (!Systems.TryGetValue(create.SystemId, out var system)) {
      throw new Exception("Jolt :: System not found");
    }

    var bi = GetBodyInterface(create.SystemId);
    var settings = new CharacterVirtualSettings() {
      Shape = create.Shape,
      MaxSlopeAngle = create.MaxSlopeAngle,
      Mass = create.Mass,
      CharacterPadding = 0.01f,
      CollisionTolerance = 0.1f,
      MaxStrength = 1.0f,
      PenetrationRecoverySpeed = 1.0f,
      PredictiveContactDistance = 1.0f,
      BackFaceMode = BackFaceMode.CollideWithBackFaces,
      Up = -Vector3.UnitY,
    };
    var pos = create.Position;

    var characterVirtual = new CharacterVirtual(settings, create.Position, create.Rotation, 0, system) {
      Mass = create.Mass,
      Up = Vector3.UnitY,
    };

    var characterId = Guid.NewGuid();
    Characters.Add(characterId, characterVirtual);

    var existing = SystemToCharacters.GetValueOrDefault(create.SystemId, []);
    SystemToCharacters[create.SystemId] = [.. existing, characterId];

    return characterId;
  }

  public CharacterVirtual GetCharacter(Guid characterId) {
    if (!Characters.TryGetValue(characterId, out var character)) {
      throw new Exception("Jolt :: Character not found");
    }

    return character;
  }

  private readonly float timestep = 1.0f / 60.0f;
  private float accumulator = 0.0f;
  private float physicsFrames = 0.0f;
  private float physicsFPSAccumulator = 0.0f;

  private IDisposable? JoltFPS;

  public void SimulateAllSystems() {
    foreach (var system in Systems) {
      SimulateFrame(system.Key);
    }
  }

  public void SimulateCharacter(Guid characterId, Guid systemId, ExtendedUpdateSettings settings = default) {
    if (!Characters.TryGetValue(characterId, out var character)) {
      logger.LogError("Jolt :: Character not found");
      return;
    }

    if (!Systems.TryGetValue(systemId, out var joltSystem)) {
      logger.LogError("Jolt :: System not found");
      return;
    }

    character.ExtendedUpdate(timestep, settings, Layers.Character, joltSystem);
  }

  public void SimulateFrame(Guid systemId) {
    if (!Systems.TryGetValue(systemId, out var joltSystem)) {
      logger.LogError("Jolt :: System not found");
      return;
    }

    physicsFrames++;
    // calculate physics fps
    physicsFPSAccumulator += (float)tm.DeltaTime.TotalMilliseconds;
    if (physicsFPSAccumulator >= 1000.0f) {
      JoltFPS?.Dispose();
      JoltFPS = LogContext.PushProperty("PFPS", physicsFrames);
      physicsFPSAccumulator = 0.0f;
      physicsFrames = 0.0f;
    }

    var frameTime = (float)Math.Clamp(tm.DeltaTime.TotalMilliseconds / 1000.0f, 0, 0.25f); // convert to seconds
    accumulator += frameTime;

    var stepCounter = 0;
    while (accumulator >= timestep) {
      var err = joltSystem.Step(timestep, 1);
      accumulator -= timestep;
      stepCounter++;

      if (err != PhysicsUpdateError.None) {
        logger.LogError("Jolt :: Error in step: {type}", err);
        break;
      }
      // Using a default settings for now. Might need to make this configurable later.


      SystemToCharacters.TryGetValue(systemId, out var characters);
      characters ??= [];

      // I think this potentially has issues when we have make up frames.
      // foreach (var id in characters) {
      //   if (!Characters.TryGetValue(id, out var character)) {
      //     logger.LogError("Jolt :: Character not found");
      //     continue;
      //   }

      //   var settings = new ExtendedUpdateSettings() {
      //     // StickToFloorStepDown = -character.Up,
      //   };

      //   character.ExtendedUpdate(timestep, settings, Layers.Character, joltSystem);
      // }

    }
  }
}
