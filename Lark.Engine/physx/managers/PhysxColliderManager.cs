using System.Numerics;
using Lark.Engine.physx.managers;
using Microsoft.Extensions.Logging;
using MagicPhysX;
using static MagicPhysX.NativeMethods;
using Lark.Engine.std;

namespace Lark.Engine.physx.managers;

public class PhysxColliderManager(ILogger<PhysxColliderManager> logger, PhysxManager pm, PhysxData physxData) {


  // GetPlaneRotation: Returns the rotation transformed from the plane equation
  public unsafe Quaternion GetPlaneRotation(Guid actorId) {
    if (!pm.ActorLookup.TryGetValue(actorId, out var actor)) {
      throw new Exception($"Actor {actorId} does not exist");
    }

    var transform = PxRigidActor_getGlobalPose((PxRigidActor*)actor.Actor);
    var plane = phys_PxPlaneEquationFromTransform(&transform);

    var normal = new Vector3(plane.n.x, plane.n.y, plane.n.z);
    var rot = LarkUtils.RotationFromNormal(normal);
    return rot;
  }

  // GetCapsuleRotation: Returns the rotation transformed from the capsule orientation
  public unsafe Quaternion GetCapsuleRotation(Guid actorId) {
    if (!pm.ActorLookup.TryGetValue(actorId, out var actor)) {
      throw new Exception($"Actor {actorId} does not exist");
    }

    var transform = PxRigidActor_getGlobalPose((PxRigidActor*)actor.Actor);

    // Capsule orientation is along the y axis, so we need to rotate the transform component to match.
    var rot = new Quaternion(transform.q.x, transform.q.y, transform.q.z, transform.q.w);
    rot *= Quaternion.CreateFromAxisAngle(-Vector3.UnitZ, MathF.PI / 2);


    return rot;
  }

  public unsafe Guid RegisterPlane(Vector3 position, Vector3 normal, Guid materialId) {
    logger.LogInformation("Physx :: Registering plane");
    var p = new PxVec3 { x = position.X, y = position.Y, z = position.Z };
    var n = new PxVec3 { x = normal.X, y = normal.Y, z = normal.Z };

    var plane = PxPlane_new_3(&p, &n);
    var transform = phys_PxTransformFromPlaneEquation(&plane);
    var identity = PxTransform_new_2(PxIDENTITY.PxIdentity);

    if (!physxData.Materials.TryGetValue(materialId, out var material)) {
      material = physxData.Materials[pm.DefaultMaterialId];
    }

    // plane has no geometry, all needed is the transform. Create empty geometry.
    var geo = PxPlaneGeometry_new();

    PxActor* physicsObject;
    physicsObject = (PxActor*)physxData.Physics->PhysPxCreateStatic(&transform, (PxGeometry*)&geo, material.Material, &identity);

    var larkActor = new LarkPhysxActor(physicsObject);
    physxData.Scene->AddActorMut(larkActor.Actor, null);

    var guid = Guid.NewGuid();
    pm.ActorLookup.Add(guid, larkActor);
    return guid;
  }

  public unsafe Guid RegisterBox(Vector3 position, Quaternion rotation, Vector3 size, bool isStatic, Guid materialId) {
    logger.LogInformation("Physx :: Registering box");
    var p = new PxVec3 { x = position.X, y = position.Y, z = position.Z };
    var s = new PxVec3 { x = size.X, y = size.Y, z = size.Z };
    var q = new PxQuat { x = rotation.X, y = rotation.Y, z = rotation.Z, w = rotation.W };
    var box = PxBoxGeometry_new_1(s);
    var transform = PxTransform_new_5(&p, &q);
    var identity = PxTransform_new_2(PxIDENTITY.PxIdentity);

    if (!physxData.Materials.TryGetValue(materialId, out var material)) {
      material = physxData.Materials[pm.DefaultMaterialId];
    }
    PxActor* physicsObject;
    if (isStatic) {
      physicsObject = (PxActor*)physxData.Physics->PhysPxCreateStatic(&transform, (PxGeometry*)&box, material.Material, &identity);
    }
    else {
      physicsObject = (PxActor*)physxData.Physics->PhysPxCreateDynamic(&transform, (PxGeometry*)&box, material.Material, 1.0f, &identity);
    }

    var larkActor = new LarkPhysxActor(physicsObject);
    physxData.Scene->AddActorMut(larkActor.Actor, null);

    var guid = Guid.NewGuid();
    pm.ActorLookup.Add(guid, larkActor);
    return guid;
  }

  public unsafe Guid RegisterSphere(Vector3 position, Quaternion rotation, float radius, bool isStatic, Guid materialId) {
    logger.LogInformation("Physx :: Registering sphere");
    var p = new PxVec3 { x = position.X, y = position.Y, z = position.Z };
    var q = new PxQuat { x = rotation.X, y = rotation.Y, z = rotation.Z, w = rotation.W };
    var sphere = PxSphereGeometry_new(radius);
    var transform = PxTransform_new_5(&p, &q);
    var identity = PxTransform_new_2(PxIDENTITY.PxIdentity);

    if (!physxData.Materials.TryGetValue(materialId, out var material)) {
      material = physxData.Materials[pm.DefaultMaterialId];
    }

    PxActor* physicsObject;
    if (isStatic) {
      physicsObject = (PxActor*)physxData.Physics->PhysPxCreateStatic(&transform, (PxGeometry*)&sphere, material.Material, &identity);
    }
    else {
      physicsObject = (PxActor*)physxData.Physics->PhysPxCreateDynamic(&transform, (PxGeometry*)&sphere, material.Material, 1.0f, &identity);
    }

    var larkActor = new LarkPhysxActor(physicsObject);
    physxData.Scene->AddActorMut(larkActor.Actor, null);

    var guid = Guid.NewGuid();
    pm.ActorLookup.Add(guid, larkActor);
    return guid;
  }

  public unsafe Guid RegisterCapsule(Vector3 position, Quaternion rotation, float radius, float height, bool isStatic, Guid materialId) {
    logger.LogInformation("Physx :: Registering capsule");
    var p = new PxVec3 { x = position.X, y = position.Y, z = position.Z };
    var q = new PxQuat { x = rotation.X, y = rotation.Y, z = rotation.Z, w = rotation.W };
    var capsule = PxCapsuleGeometry_new(radius, height / 2);
    var transform = PxTransform_new_5(&p, &q);
    var identity = PxTransform_new_2(PxIDENTITY.PxIdentity);

    if (!physxData.Materials.TryGetValue(materialId, out var material)) {
      material = physxData.Materials[pm.DefaultMaterialId];
    }

    PxActor* physicsObject;
    if (isStatic) {
      physicsObject = (PxActor*)physxData.Physics->PhysPxCreateStatic(&transform, (PxGeometry*)&capsule, material.Material, &identity);
    }
    else {
      physicsObject = (PxActor*)physxData.Physics->PhysPxCreateDynamic(&transform, (PxGeometry*)&capsule, material.Material, 1.0f, &identity);
    }

    var larkActor = new LarkPhysxActor(physicsObject);
    physxData.Scene->AddActorMut(larkActor.Actor, null);

    var guid = Guid.NewGuid();
    pm.ActorLookup.Add(guid, larkActor);
    return guid;
  }
}