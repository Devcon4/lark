using System.Numerics;
using System.Runtime.InteropServices;
using Lark.Engine.ecs;
using MagicPhysX;
using Microsoft.Extensions.Logging;

using static MagicPhysX.NativeMethods;

namespace Lark.Engine.physx.managers;

public class PhysxCharacterManager(ILogger<PhysxCharacterManager> Logger, PhysxData Data, PhysxManager pm) : LarkManager {

  public Dictionary<Guid, LarkPhsyxController> Controllers { get; private set; } = [];

  public Dictionary<Guid, Guid> EntityToController { get; private set; } = [];

  public unsafe Guid CreateController(Guid entityId, float radius, float height, Vector3 position, Guid? materialId = null) {
    Logger.LogInformation("PhysxControllerManager :: CreateController");

    materialId ??= pm.DefaultMaterialId;

    var controllerDesc = PxCapsuleControllerDesc_new_alloc();
    controllerDesc->material = pm.GetMaterial(materialId.Value).Material;
    controllerDesc->radius = radius;
    controllerDesc->height = height;
    controllerDesc->upDirection = -Vector3.UnitY;
    controllerDesc->position = PxExtendedVec3_new_1(position.X, position.Y, position.Z);

    var controllerId = Guid.NewGuid();

    var controller = PxControllerManager_createController_mut(Data.ControllerManager, (PxControllerDesc*)controllerDesc);

    // Setup static object filter
    var filterData = PxFilterData_new(PxEMPTY.PxEmpty);
    var filterQuery = PxQueryFilterData_new_1(&filterData, PxQueryFlags.Static);

    var filter = PxControllerFilters_new((PxFilterData*)&filterQuery, null, null);
    var filterPtr = Marshal.AllocHGlobal(Marshal.SizeOf<PxControllerFilters>());
    Marshal.StructureToPtr(filter, filterPtr, false);

    Controllers.Add(controllerId, new LarkPhsyxController(controller, (PxControllerFilters*)filterPtr));
    EntityToController.Add(entityId, controllerId);

    Logger.LogInformation("PhysxControllerManager :: Created controller with id {controllerId}", controllerId);
    return controllerId;
  }

  public bool HasController(Guid entityId) {
    return EntityToController.ContainsKey(entityId);
  }

  public unsafe Vector3 GetPosition(Guid entityId) {
    if (!EntityToController.TryGetValue(entityId, out var controllerId)) {
      throw new Exception($"Entity {entityId} does not have a controller");
    }

    if (!Controllers.TryGetValue(controllerId, out var controller)) {
      throw new Exception($"Controller {controllerId} does not exist");
    }

    var transform = *controller.Controller->GetPosition();
    return new Vector3((float)transform.x, (float)transform.y, (float)transform.z);
  }

  public unsafe Vector3 GetFootPosition(Guid entityId) {
    if (!EntityToController.TryGetValue(entityId, out var controllerId)) {
      throw new Exception($"Entity {entityId} does not have a controller");
    }

    if (!Controllers.TryGetValue(controllerId, out var controller)) {
      throw new Exception($"Controller {controllerId} does not exist");
    }

    var transform = controller.Controller->GetFootPosition();
    return new Vector3((float)transform.x, (float)transform.y, (float)transform.z);
  }

  // move
  public unsafe void Move(Guid entityId, Vector3 displacement, float elapsedTime, float minDistance = 0.0001f) {
    if (!EntityToController.TryGetValue(entityId, out var controllerId)) {
      throw new Exception($"Entity {entityId} does not have a controller");
    }

    if (!Controllers.TryGetValue(controllerId, out var controller)) {
      throw new Exception($"Controller {controllerId} does not exist");
    }

    PxVec3 dis = new() { x = displacement.X, y = displacement.Y, z = displacement.Z };
    // controller.Controller->MoveMut(&dis, minDistance, elapsedTime, controller.Filters, null);
    PxController_move_mut(controller.Controller, &dis, float.Epsilon, elapsedTime, controller.Filters, null);
  }
}

public unsafe class LarkPhsyxController(PxController* Controller, PxControllerFilters* Filters) {
  public PxController* Controller = Controller;
  public PxControllerFilters* Filters = Filters;
}