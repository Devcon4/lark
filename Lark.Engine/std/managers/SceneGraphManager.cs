
using System.Collections.Frozen;
using System.Numerics;
using Lark.Engine.ecs;

namespace Lark.Engine.std;

public class SceneGraphManager(EntityManager em) : LarkManager {
  public Dictionary<Guid, FrozenSet<Guid>> Nodes { get; private set; } = [];

  // InverseGraph: Lookup which returns the branch of the tree for each leaf node
  public Dictionary<Guid, FrozenSet<Guid>> InverseGraph { get; private set; } = [];

  public void AddNode(Guid parent, Guid child) {
    if (!Nodes.ContainsKey(parent)) {
      Nodes[parent] = new HashSet<Guid>().ToFrozenSet();
    }

    if (Nodes.TryGetValue(parent, out var children)) {
      var newSet = children.ToHashSet();
      newSet.Add(child);
      Nodes[parent] = newSet.ToFrozenSet();
    }

    if (!InverseGraph.ContainsKey(child)) {
      InverseGraph[child] = new HashSet<Guid>().ToFrozenSet();
    }

    if (InverseGraph.TryGetValue(child, out var parents)) {
      var newSet = parents.ToHashSet();
      newSet.Add(parent);
      InverseGraph[child] = newSet.ToFrozenSet();
    }
  }

  public void RemoveNode(Guid parent, Guid child) {
    if (Nodes.TryGetValue(parent, out var children)) {
      // If there is no children, remove the parent
      if (children.Count == 1) {
        Nodes.Remove(parent);
        return;
      }

      var newSet = children.ToHashSet();
      newSet.Remove(child);
      Nodes[parent] = newSet.ToFrozenSet();
    }

    if (InverseGraph.TryGetValue(child, out var parents)) {
      // If there is no parents, remove the child
      if (parents.Count == 1) {
        InverseGraph.Remove(child);
        return;
      }

      var newSet = parents.ToHashSet();
      newSet.Remove(parent);
      InverseGraph[child] = newSet.ToFrozenSet();
    }
  }

  public void UpdateNode(Guid child, Guid newParent) {
    if (InverseGraph.TryGetValue(child, out var parents)) {
      foreach (var parent in parents) {
        RemoveNode(parent, child);
      }
    }

    AddNode(newParent, child);
  }

  // HasNode returns true if the given node is a child of the given parent
  public bool HasNode(Guid parent, Guid child) {
    if (Nodes.TryGetValue(parent, out var children)) {
      return children.Contains(child);
    }

    return false;
  }

  // HasNode returns true if the given node is in the graph at all
  public bool HasNode(Guid node) {
    return Nodes.ContainsKey(node) || InverseGraph.ContainsKey(node);
  }

  // GetBranch returns the branch of the tree for the given leaf node
  public FrozenSet<Guid> GetBranch(Guid leaf) {
    if (InverseGraph.TryGetValue(leaf, out var parents)) {
      return parents;
    }

    return new HashSet<Guid>().ToFrozenSet();
  }

  public void UpdateGlobalTransforms() {
    foreach (var (entityId, components) in em.GetEntitiesWithComponentsSync(typeof(GlobalTransformComponent))) {
      var globalTransform = components.Get<TransformComponent>();

      foreach (var ancestor in GetBranch(entityId)) {
        var (_, ac) = em.GetEntity(ancestor);
        var transform = ac.Get<TransformComponent>();
        globalTransform = CombineTransforms(globalTransform, transform);
      }

      em.UpdateEntityComponent(entityId, new GlobalTransformComponent(globalTransform.Position, globalTransform.Scale, globalTransform.Rotation));
    }
  }

  private TransformComponent CombineTransforms(TransformComponent a, TransformComponent b) {
    // Combine two TransformComponent instances
    // var position = a.Position + b.Position;
    // var scale = a.Scale * b.Scale;
    // var rotation = a.Rotation * b.Rotation;

    // Example: if a child is positioned [-2,0,0] to parent [0,0,0], and the parent rotates 90 degrees, we need to transform the child. In otherwords the child is positioned relative to the parent.
    // Our rotation also is relative to the parent, so we need to transform the child's rotation as well.
    // scale is not effected.
    var position = a.Position + Vector3.Transform(b.Position, a.Rotation);
    var rotation = a.Rotation * b.Rotation;
    var scale = b.Scale;


    return new TransformComponent(position, scale, rotation);
  }

}