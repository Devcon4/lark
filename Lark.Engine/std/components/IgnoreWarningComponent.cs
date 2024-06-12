using Lark.Engine.ecs;

namespace Lark.Engine.std.components;
public record struct LarkIgnoreWarningComponent(string Code, string? Description) : ILarkComponent;