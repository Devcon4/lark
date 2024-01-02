
using System.Collections.Frozen;
using Lark.Engine.ecs;
using Microsoft.Extensions.Logging;
using Silk.NET.Core;

namespace Lark.Engine.std;

public record struct ActionMapComponent(string ActionName, ILarkActionTrigger Trigger) : ILarkComponent { }

// User input needs to run outside of the traditional system loop.
// ActionSystem still has the same general structure and acts like a system, but is called from the ActionManager when an InputManager callback is triggered.

// TODO: Rework this so rather than being a system it uses the straight input callbacks.
// I want actions to not be linked to framerate. 
// Action maps also need to be reworked. I should be able to pass a name and action func and seperately associate an action to a trigger.
// That will allow the triggers to be swapped out or remapped.
// public class ActionSystem(EntityManager em, ActionManager am, ILogger<ActionSystem> logger) : ILarkActionSystem {
//   public Type[] RequiredComponents => [typeof(ActionComponent)];

//   public Task Init() {
//     em.AddEntity(new SystemComponent(), new MetadataComponent("ActionMap"), new LarkMapComponent(ActionManager.DefaultMap, true, []));
//     return Task.CompletedTask;
//   }

//   public void Update((Guid, FrozenSet<ILarkComponent>) Entity) {
    

//   }
// }