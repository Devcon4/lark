using ImpromptuNinjas.UltralightSharp;
using Lark.Engine.ecs;
using Microsoft.Extensions.Logging;
using ultralightString = ImpromptuNinjas.UltralightSharp.String;

namespace Lark.Engine.Ultralight;

public class UltralightManager(UltralightController uc, ILogger<UltralightManager> logger) : LarkManager
{

  public unsafe void Navigate(string url)
  {
    // uses trhe history api to navigate to a new url in the ultralight view. executes javascript.
    var str = ultralightString.Create($"window.history.pushState(null, null, '{url}');");
    ultralightString** ex = null;
    uc.View->EvaluateScript(str, ex);

    if (ex is not null)
    {
      logger.LogError("Error navigating to url: {error}", (*ex)->Read());
    }

    str->Destroy();
  }
}