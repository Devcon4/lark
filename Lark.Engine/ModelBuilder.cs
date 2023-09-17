using SharpGLTF;
using SharpGLTF.Schema2;
using SharpGLTF.Validation;
namespace Lark.Engine;

public class ModelBuilder {

  public ModelBuilder() { }

  public void Load(string modelName) {
    var model = ModelRoot.Load(modelName, ValidationMode.TryFix);
  }
}