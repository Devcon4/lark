using System.Numerics;
using AutoFixture;
using FluentAssertions;
using Lark.Engine.gi;

namespace Lark.UnitTests;

public class OctreeUtilsTests {

  [Fact]
  public void RegisterProbeGroup_HasCorrectNumber() {
    // Arrange
    var boundingBox = new Vector3(2f, 2f, 2f);
    var density = 1f;
    var position = new Vector3(0, 0, 0);

    var octree = new LarkOctree();

    // Act
    octree.RegisterProbeGroup(boundingBox, density, position);

    // Assert
    octree.Probes.Should().NotBeEmpty();
    octree.Probes.Length.Should().Be(27);
  }

  [Fact]
  public void RegisterProbeGroup_ProbesWithinBoundingBox() {
    // Arrange
    var boundingBox = new Vector3(2f, 2f, 2f);
    var density = 1f;
    var position = new Vector3(0, 0, 0);
    var octree = new LarkOctree();

    // Act
    octree.RegisterProbeGroup(boundingBox, density, position);

    // Assert
    octree.Probes.Should().NotBeEmpty();
    octree.Probes.All(p => Math.Abs(p.X) <= boundingBox.X / 2).Should().BeTrue();
    octree.Probes.All(p => Math.Abs(p.Y) <= boundingBox.Y / 2).Should().BeTrue();
    octree.Probes.All(p => Math.Abs(p.Z) <= boundingBox.Z / 2).Should().BeTrue();
  }

  [Fact]
  public void RegisterProbeGroup_ProbesSpacedByDensity() {
    // Arrange
    var boundingBox = new Vector3(2f, 2f, 2f);
    var density = 1f;
    var position = new Vector3(0, 0, 0);
    var octree = new LarkOctree();

    // Act
    octree.RegisterProbeGroup(boundingBox, density, position);

    // Assert
    octree.Probes.Should().NotBeEmpty();
    octree.Probes.All(p => Math.Abs(p.X % density) < 0.0001).Should().BeTrue();
    octree.Probes.All(p => Math.Abs(p.Y % density) < 0.0001).Should().BeTrue();
    octree.Probes.All(p => Math.Abs(p.Z % density) < 0.0001).Should().BeTrue();
  }

  [Fact]
  public void RegisterProbeGroup_OffsetByPosition() {
    // Arrange
    var boundingBox = new Vector3(2f, 2f, 2f);
    var density = 1f;
    var position = new Vector3(1, 1, 1);
    var octree = new LarkOctree();

    // Act
    octree.RegisterProbeGroup(boundingBox, density, position);

    // Assert
    octree.Probes.Should().NotBeEmpty();
    octree.Probes.All(p => Math.Abs(p.X - position.X) <= boundingBox.X / 2).Should().BeTrue();
    octree.Probes.All(p => Math.Abs(p.Y - position.Y) <= boundingBox.Y / 2).Should().BeTrue();
    octree.Probes.All(p => Math.Abs(p.Z - position.Z) <= boundingBox.Z / 2).Should().BeTrue();
  }

  [Fact]
  public void LargestFittingBoundingBox_ReturnsCorrectBoundingBox() {
    // Arrange
    var fixture = new Fixture();
    var probes = fixture.CreateMany<Vector3>(10).ToList();
    var octree = new LarkOctree() {
      Probes = [.. probes]
    };

    // Act
    var largestBoundingBox = octree.LargestFittingBoundingBox();

    // Assert
    largestBoundingBox.Should().NotBeNull();
    largestBoundingBox.X.Should().Be(probes.Max(p => Math.Abs(p.X)));
    largestBoundingBox.Y.Should().Be(probes.Max(p => Math.Abs(p.Y)));
    largestBoundingBox.Z.Should().Be(probes.Max(p => Math.Abs(p.Z)));
  }

  [Fact]
  public void GenerateOctree_CreatesOct() {
    var octree = new LarkOctree() { };

    octree.RegisterProbeGroup(new(10, 10, 10), 1, Vector3.Zero);
    octree.Build();

    octree.Should().NotBeNull();
    octree.Root.Should().NotBeNull();

    var testPos = new Vector3(0, 1, 0);
    var node = octree.Test(testPos);

    node.Should().NotBeNull();
  }

  [Fact]
  public void TestOctree_ReturnsCorrectNode() {
    // Arrange
    var octree = new LarkOctree() {
      MaxLeafProbes = 400
    };

    octree.RegisterProbeGroup(new(10, 10, 10), 1, Vector3.Zero);
    octree.Build();

    // Act
    octree.Should().NotBeNull();
    octree.Root.Should().NotBeNull();

    var testPos = new Vector3(0, 1, 0);
    var node = octree.Test(testPos);

    // Assert
    node.Should().NotBeNull();
    LarkOctreeNode testNode = (LarkOctreeNode)node!;

    // Node position + bounding box should contain the test position
    var xCheck = testNode.Position.X + testNode.BoundingBox.X >= testPos.X && testNode.Position.X - testNode.BoundingBox.X <= testPos.X;
    var yCheck = testNode.Position.Y + testNode.BoundingBox.Y >= testPos.Y && testNode.Position.Y - testNode.BoundingBox.Y <= testPos.Y;
    var zCheck = testNode.Position.Z + testNode.BoundingBox.Z >= testPos.Z && testNode.Position.Z - testNode.BoundingBox.Z <= testPos.Z;

    xCheck.Should().BeTrue();
    yCheck.Should().BeTrue();
    zCheck.Should().BeTrue();

    node!.Value.IsLeaf.Should().BeTrue();
    node.Value.ProbeIndexes.Should().NotBeEmpty();
  }

  [Fact]
  public void TestOctree_LargeOctree() {
    // Arrange
    var octree = new LarkOctree() {
      MaxLeafProbes = 1024,
      MaxDepth = 50
    };

    octree.RegisterProbeGroup(new(100, 50, 100), 1f, Vector3.Zero);
    octree.Build();

    octree.Should().NotBeNull();
    octree.Root.Should().NotBeNull();

    var testPos = new Vector3(1, -1, 1);
    var node = octree.Test(testPos);

    // Assert
    node.Should().NotBeNull();
    LarkOctreeNode testNode = (LarkOctreeNode)node!;

    // Node position + bounding box should contain the test position
    var xCheck = testNode.Position.X + testNode.BoundingBox.X >= testPos.X && testNode.Position.X - testNode.BoundingBox.X <= testPos.X;
    var yCheck = testNode.Position.Y + testNode.BoundingBox.Y >= testPos.Y && testNode.Position.Y - testNode.BoundingBox.Y <= testPos.Y;
    var zCheck = testNode.Position.Z + testNode.BoundingBox.Z >= testPos.Z && testNode.Position.Z - testNode.BoundingBox.Z <= testPos.Z;

    xCheck.Should().BeTrue();
    yCheck.Should().BeTrue();
    zCheck.Should().BeTrue();

    node!.Value.IsLeaf.Should().BeTrue();
    node.Value.ProbeIndexes.Should().NotBeEmpty();
  }

  [Theory]
  [InlineData(0.1f, -0.1f, 0.1f, 7, .5f, -.5f, .5f)]
  [InlineData(-0.1f, 0.1f, 0.1f, 4, -.5f, .5f, .5f)]
  [InlineData(0.1f, 0.1f, 0.1f, 5, .5f, .5f, .5f)]
  [InlineData(-0.1f, -0.1f, 0.1f, 6, -.5f, -.5f, .5f)]
  [InlineData(0.1f, -0.1f, -0.1f, 3, .5f, -.5f, -.5f)]
  [InlineData(-0.1f, 0.1f, -0.1f, 0, -.5f, .5f, -.5f)]
  [InlineData(0.1f, 0.1f, -0.1f, 1, .5f, .5f, -.5f)]
  [InlineData(-0.1f, -0.1f, -0.1f, 2, -.5f, -.5f, -.5f)]
  public void DetermineChildIndexAndCalculateChildPosition_ShouldAlign(float posX, float posY, float posZ, int expectedIndex, float childX, float childY, float childZ) {
    var testPos = new Vector3(posX, posY, posZ);
    var childPos = new Vector3(childX, childY, childZ);
    var testNode = new LarkOctreeNode(Vector3.Zero, Vector3.One);

    var childIndex = LarkOctree.DetermineChildIndex(testNode.Position, testPos);
    var childPositions = LarkOctree.CalculateChildPositions(testNode.Position, testNode.BoundingBox);

    childIndex.Should().Be(expectedIndex);
    childPositions[childIndex].Should().Be(childPos);
  }
}