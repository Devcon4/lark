using System.Numerics;
using Lark.Engine.ecs;

namespace Lark.Engine.physx.components;

public record struct PhysxWorldComponent(string WorldName, Vector3 Gravity) : ILarkComponent { }
