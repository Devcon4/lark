
using System.Collections.Frozen;
using Lark.Engine.ecs;

namespace Lark.Engine.std;

public class SceneGraphManager : LarkManager {
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


}