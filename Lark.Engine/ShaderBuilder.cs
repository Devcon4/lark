
using System.Reflection;
using System.Resources;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lark.Engine {

  // Waiting for Silk.NET 2.18.0 which will have spirv support.
  public class ShaderBuilder {

    private readonly ILogger<ShaderBuilder> _logger;

    public ShaderBuilder(ILogger<ShaderBuilder> logger) {
      _logger = logger;
    }

    public byte[] LoadShader(string shaderName) {
      _logger.LogInformation("Loading shader {ShaderName}", shaderName);
      var path = Path.Join(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"./resources/{shaderName}.spv");

      if (!File.Exists(path)) {
        _logger.LogError("Shader {ShaderName} does not exist.", shaderName);
        throw new FileNotFoundException($"Shader {shaderName} does not exist.");
      }

      var content = File.ReadAllBytes(path);

      if (content.Length == 0) {
        _logger.LogError("Shader {ShaderName} is empty.", shaderName);
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