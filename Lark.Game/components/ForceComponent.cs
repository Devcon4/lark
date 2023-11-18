using System.Numerics;
using Lark.Engine.ecs;

namespace Lark.Game.components;

public record struct ForceComponent(Vector3 Force) : ILarkComponent;