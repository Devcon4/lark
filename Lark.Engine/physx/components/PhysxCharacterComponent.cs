using Lark.Engine.ecs;

namespace Lark.Engine.physx;

public record struct PhysxCharacterComponent(float Radius, float Height, Guid? ControllerId = null) : ILarkComponent { }