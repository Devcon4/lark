using Lark.Engine.ecs;

namespace Lark.Engine.physx.components;
public record struct PhysxMaterialComponent(float StaticFriction, float DynamicFriction, float Restitution) : ILarkComponent { }
