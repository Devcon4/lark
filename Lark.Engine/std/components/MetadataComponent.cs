using Lark.Engine.ecs;

namespace Lark.Engine.std;

public record struct MetadataComponent(string Name, bool Enabled = true) : ILarkComponent;