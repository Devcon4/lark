using System.Collections.Frozen;
using System.Numerics;
using JoltPhysicsSharp;
using Lark.Engine.ecs;
using Lark.Engine.model;
using Silk.NET.OpenGL;

namespace Lark.Engine.gi;

public class LightProbeManager : LarkManager {
  internal Vector3 BoundingBox = new(100, 50, 100);
  internal float Density = 1f;
  internal LarkOctree DirectOctree = new();
  internal LarkOctree IndirectOctree = new();

  public Dictionary<Guid, ILarkLight> Lights = [];
  public Dictionary<Vector3, Guid> LightPositions = [];

  internal bool NeedsUpdate = false;
  public override Task Init() {
    GenerateDefaultProbes(Density);
    DirectOctree.Build();
    IndirectOctree.Build();
    return Task.CompletedTask;
  }

  internal void GenerateDefaultProbes(float Density = 1f) {
    IndirectOctree.RegisterProbeGroup(BoundingBox, Density, Vector3.Zero);
  }

  public IEnumerable<ILarkLight> NearbyDirectLights(Vector3 position) {
    var node = DirectOctree.Test(position);
    if (node is null) yield break;

    // Get all the lights in the node
    foreach (var light in node.Value.ProbeIndexes) {
      yield return Lights[LightPositions[DirectOctree.Probes[light]]];
    }
  }

  public void RegisterLight(Vector3 position) {
    // Register a light source with the probe manager
    DirectOctree.RegisterProbe(position);

    NeedsUpdate = true;
  }

  public void UpdateDirectProbes() {
    DirectOctree.Build();
    NeedsUpdate = false;
  }

  public void MoveLight(Vector3 lastPosition, Vector3 position) {
    DirectOctree.RemoveProbe(lastPosition);
    DirectOctree.RegisterProbe(position);

    NeedsUpdate = true;
  }
}

public class LarkOctree {
  public LarkOctreeNode Root;
  public Vector3[] Probes = [];
  public int MaxDepth = 50;
  public int MaxLeafProbes = 512;

  public Vector3 LargestFittingBoundingBox() {
    var largest = Vector3.Zero;
    foreach (var position in Probes) {
      largest = Vector3.Max(largest, position);
    }

    return largest;
  }

  // For the given position, recurse through the octree to find the deepest node that contains the position. Return the probe indexes in that node.
  public LarkOctreeNode? Test(Vector3 position) {
    var node = Root;
    while (!node.IsLeaf) {
      var index = DetermineChildIndex(node.Position, position);
      if (node.Children is null) return null;
      node = node.Children[index];
    }

    return node;
  }

  public void Build() {
    Memory<Vector3> probeMem = Probes.ToArray();
    var sceneBoundingBox = LargestFittingBoundingBox();
    Root = new LarkOctreeNode(Vector3.Zero, sceneBoundingBox, Probes.Select((_, i) => i).ToHashSet());

    // Start subdivision process
    SubdivideNode(ref Root, probeMem);
  }

  internal void SubdivideNode(ref LarkOctreeNode node, Memory<Vector3> probes, int depth = 0) {
    if (node.ProbeIndexes.Count <= MaxLeafProbes) return; // Base case: node has acceptable number of probes
    if (node.ProbeIndexes.Count == 0) return; // Base case: node has no probes

    // Calculate new size for child nodes (1/4 the size of the parent node)
    var newSize = node.BoundingBox / 2;
    var childPositions = CalculateChildPositions(node.Position, newSize);


    node.Children = Enumerable.Range(0, 8).Select(i => new LarkOctreeNode(childPositions[i], newSize)).ToArray();

    foreach (var probe in node.ProbeIndexes) {
      int childIndex = DetermineChildIndex(node.Position, probes.Span[probe]);
      node.Children[childIndex].ProbeIndexes.Add(probe);
    }

    // Clear probes from current node as they have been distributed to children
    node.ProbeIndexes.Clear();

    // calc octree depth
    if (depth >= MaxDepth) return;

    depth++;

    // Recursively subdivide child nodes
    for (int i = 0; i < node.Children.Length; i++) {
      SubdivideNode(ref node.Children[i], probes, depth);
    }
  }

  public void RegisterProbe(Vector3 position) {
    Probes = [.. Probes, position];
  }

  public void RemoveProbe(Vector3 position) {
    Probes = Probes.Where(p => p != position).ToArray();
  }

  public void RegisterProbeGroup(Vector3 BoundingBox, float Density, Vector3 Position) {
    // Generate probes within the bounding box, spaced by the density and offset by the position. Add to the probe set
    // given the starting point of position, which is the center of the bounding box, compute how many density steps we need to reach the edge.
    var half = BoundingBox / 2;
    half = new Vector3(
      (float)Math.Ceiling(half.X / Density) * Density,
      (float)Math.Ceiling(half.Y / Density) * Density,
      (float)Math.Ceiling(half.Z / Density) * Density
    );

    // Calculate the number of probes we need to generate for the given bounding box and density in each axis
    var probeCount = (int)Math.Ceiling((half.X * 2 + 1) * (half.Y * 2 + 1) * (half.Z * 2 + 1) / Density);
    Memory<Vector3> newProbes = new Vector3[probeCount];
    var handler = newProbes.Pin();

    var localIndex = 0;
    for (var x = -half.X; x <= half.X; x += Density) {
      for (var y = -half.Y; y <= half.Y; y += Density) {
        for (var z = -half.Z; z <= half.Z; z += Density) {
          newProbes.Span[localIndex++] = new Vector3(x, y, z) + Position;
        }
      }
    }

    HashSet<Vector3> finalSet = [.. Probes, .. newProbes.Span];
    Probes = [.. finalSet];

    handler.Dispose();
  }

  internal static int DetermineChildIndex(Vector3 nodePosition, Vector3 probePosition) {
    int index = 0;

    // Determine the octant the probe belongs to
    if (probePosition.X >= nodePosition.X) index |= 1; // Right half
    if (probePosition.Y < nodePosition.Y) index |= 2; // Top half
    if (probePosition.Z >= nodePosition.Z) index |= 4; // Front half

    // index: 0 = Left bottom back
    // index: 1 = Right bottom back
    // index: 2 = Left top back
    // index: 3 = Right top back
    // index: 4 = Left bottom front
    // index: 5 = Right bottom front
    // index: 6 = Left top front
    // index: 7 = Right top front

    return index;
  }

  internal static Vector3[] CalculateChildPositions(Vector3 parentPosition, Vector3 newSize) {
    Vector3[] childPositions = new Vector3[8];

    float offsetX = newSize.X / 2;
    float offsetY = newSize.Y / 2;
    float offsetZ = newSize.Z / 2;

    // Generate positions for all 8 children

    // Left top back (higher Y value in engine, but considered lower due to -y up)
    childPositions[0] = parentPosition + new Vector3(-offsetX, offsetY, -offsetZ);
    // Right top back
    childPositions[1] = parentPosition + new Vector3(offsetX, offsetY, -offsetZ);
    // Left bottom back (lower Y value in engine, but considered higher due to -y up)
    childPositions[2] = parentPosition + new Vector3(-offsetX, -offsetY, -offsetZ);
    // Right bottom back
    childPositions[3] = parentPosition + new Vector3(offsetX, -offsetY, -offsetZ);
    // Left top front
    childPositions[4] = parentPosition + new Vector3(-offsetX, offsetY, offsetZ);
    // Right top front
    childPositions[5] = parentPosition + new Vector3(offsetX, offsetY, offsetZ);
    // Left bottom front
    childPositions[6] = parentPosition + new Vector3(-offsetX, -offsetY, offsetZ);
    // Right bottom front
    childPositions[7] = parentPosition + new Vector3(offsetX, -offsetY, offsetZ);

    return childPositions;
  }
}

public record struct LarkOctreeNode(Vector3 Position, Vector3 BoundingBox, HashSet<int>? ProbeIndexes = null) {
  public HashSet<int> ProbeIndexes = ProbeIndexes ?? [];
  public LarkOctreeNode[]? Children = null;
  public readonly bool IsLeaf => Children == null;
}