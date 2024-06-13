
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Lark.Engine {

  // Waiting for Silk.NET 2.18.0 which will have spirv support.
  public class ShaderBuilder(ILogger<ShaderBuilder> logger) {
    public byte[] LoadShader(string shaderName) {
      var path = Path.Join(Path.GetDirectoryName(AppContext.BaseDirectory), $"./resources/shaders/{shaderName}.spv");

      if (!File.Exists(path)) {
        logger.LogError("Shader {ShaderName} does not exist at {ShaderPath}", shaderName, path);
        throw new FileNotFoundException($"Shader {shaderName} does not exist at {path}");
      }

      var content = File.ReadAllBytes(path);

      if (content.Length == 0) {
        logger.LogError("Shader {ShaderName} is empty.", shaderName);
        throw new FileLoadException($"Shader {shaderName} is empty.");
      }

      return content;
    }
  }

  public struct LarkShaderInfo {
    public string Name;
    public unsafe byte[] Code;
    public nuint Size;
  }
}