using JoltPhysicsSharp;
using Lark.Engine.ecs;
using Lark.Engine.jolt.managers;

namespace Lark.Engine.jolt.components;

public record struct JoltBodyInstanceComponent(Guid SystemId, BodyID BodyId) : ILarkComponent;

public record struct JoltSystemComponent(Guid SystemId) : ILarkComponent;

public record struct JoltBodyComponent(ShapeSettings Shape, MotionType MotionType, ObjectLayer ObjectLayer) : ILarkComponent;

public record struct JoltCharacterComponent(CapsuleShape Shape, float MaxSlopeAngle, float Mass) : ILarkComponent;

public record struct JoltCharacterInstance(Guid CharacterId, Guid SystemId) : ILarkComponent;

public record struct JoltConstraintComponent(Guid ParentEntityId, TwoBodyConstraintSettings Settings, Guid? SystemId) : ILarkComponent;

public record struct JoltConstraintInstance(Guid ConstraintId) : ILarkComponent;
