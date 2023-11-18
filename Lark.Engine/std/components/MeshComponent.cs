using Lark.Engine.ecs;

namespace Lark.Engine.std;

public record struct MeshComponent(string Path) : ILarkComponent;
