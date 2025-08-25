// Copyright (c) Microsoft. All rights reserved.

using System.Linq;
using Microsoft.SemanticKernel.Plugins.AtomSpace;
using Xunit;

namespace SemanticKernel.Plugins.UnitTests.AtomSpace;

public class AtomSpaceQueryTests
{
    private readonly Microsoft.SemanticKernel.Plugins.AtomSpace.AtomSpace _atomSpace;

    public AtomSpaceQueryTests()
    {
        _atomSpace = new Microsoft.SemanticKernel.Plugins.AtomSpace.AtomSpace();
        
        // Setup test data
        var node1 = new Node("node1", "TestNode", ScaleLevel.Cellular);
        var node2 = new Node("node2", "CellularComponent", ScaleLevel.Cellular);
        var node3 = new Node("node3", "MolecularComponent", ScaleLevel.Molecular);
        var skinNode1 = new SkinNode("skin1", "Keratinocyte", ScaleLevel.Cellular, SkinComponentType.Keratinocyte);
        var skinNode2 = new SkinNode("skin2", "Protein", ScaleLevel.Molecular, SkinComponentType.Protein);
        var link1 = new Link("link1", "Contains", ScaleLevel.Cellular, new[] { node1, node2 });
        var link2 = new Link("link2", "ComposedOf", ScaleLevel.Tissue, new[] { skinNode1, skinNode2 });

        _atomSpace.AddAtom(node1);
        _atomSpace.AddAtom(node2);
        _atomSpace.AddAtom(node3);
        _atomSpace.AddAtom(skinNode1);
        _atomSpace.AddAtom(skinNode2);
        _atomSpace.AddAtom(link1);
        _atomSpace.AddAtom(link2);
    }

    [Fact]
    public void Query_OfType_FiltersCorrectly()
    {
        // Act
        var nodes = _atomSpace.Query().OfType(AtomType.Node).Execute().ToList();
        var links = _atomSpace.Query().OfType(AtomType.Link).Execute().ToList();

        // Assert
        Assert.Equal(5, nodes.Count); // 3 regular nodes + 2 skin nodes
        Assert.Equal(2, links.Count);
    }

    [Fact]
    public void Query_AtScale_FiltersCorrectly()
    {
        // Act
        var cellularAtoms = _atomSpace.Query().AtScale(ScaleLevel.Cellular).Execute().ToList();
        var molecularAtoms = _atomSpace.Query().AtScale(ScaleLevel.Molecular).Execute().ToList();

        // Assert
        // Cellular: node1, node2, skinNode1, link1 = 4 atoms
        Assert.Equal(4, cellularAtoms.Count);
        // Molecular: node3, skinNode2 = 2 atoms  
        Assert.Equal(2, molecularAtoms.Count);
    }

    [Fact]
    public void Query_WithNameContaining_FiltersCorrectly()
    {
        // Act
        var testNodes = _atomSpace.Query().WithNameContaining("Test").Execute().ToList();
        var cellularNodes = _atomSpace.Query().WithNameContaining("Cellular").Execute().ToList();

        // Assert
        Assert.Single(testNodes);
        Assert.Equal("TestNode", ((Node)testNodes[0]).Name);
        Assert.Single(cellularNodes);
        Assert.Equal("CellularComponent", ((Node)cellularNodes[0]).Name);
    }

    [Fact]
    public void Query_WithLinkType_FiltersCorrectly()
    {
        // Act
        var containsLinks = _atomSpace.Query().WithLinkType("Contains").Execute().ToList();
        var composedOfLinks = _atomSpace.Query().WithLinkType("ComposedOf").Execute().ToList();

        // Assert
        Assert.Single(containsLinks);
        Assert.Equal("Contains", ((Link)containsLinks[0]).LinkType);
        Assert.Single(composedOfLinks);
        Assert.Equal("ComposedOf", ((Link)composedOfLinks[0]).LinkType);
    }

    [Fact]
    public void Query_WithSkinComponent_FiltersCorrectly()
    {
        // Act
        var keratinocytes = _atomSpace.Query().WithSkinComponent(SkinComponentType.Keratinocyte).Execute().ToList();
        var proteins = _atomSpace.Query().WithSkinComponent(SkinComponentType.Protein).Execute().ToList();

        // Assert
        Assert.Single(keratinocytes);
        Assert.Equal(SkinComponentType.Keratinocyte, ((SkinNode)keratinocytes[0]).ComponentType);
        Assert.Single(proteins);
        Assert.Equal(SkinComponentType.Protein, ((SkinNode)proteins[0]).ComponentType);
    }

    [Fact]
    public void Query_WithMinStrength_FiltersCorrectly()
    {
        // Arrange
        var highStrengthNode = new Node("high", "HighStrength", ScaleLevel.Cellular, null, new TruthValue(0.9, 0.8));
        var lowStrengthNode = new Node("low", "LowStrength", ScaleLevel.Cellular, null, new TruthValue(0.3, 0.8));
        _atomSpace.AddAtom(highStrengthNode);
        _atomSpace.AddAtom(lowStrengthNode);

        // Act
        var highStrengthAtoms = _atomSpace.Query().WithMinStrength(0.5).Execute().ToList();

        // Assert
        Assert.Contains(highStrengthNode, highStrengthAtoms);
        Assert.DoesNotContain(lowStrengthNode, highStrengthAtoms);
    }

    [Fact]
    public void Query_WithMinConfidence_FiltersCorrectly()
    {
        // Arrange
        var highConfidenceNode = new Node("high", "HighConfidence", ScaleLevel.Cellular, null, new TruthValue(0.5, 0.9));
        var lowConfidenceNode = new Node("low", "LowConfidence", ScaleLevel.Cellular, null, new TruthValue(0.5, 0.3));
        _atomSpace.AddAtom(highConfidenceNode);
        _atomSpace.AddAtom(lowConfidenceNode);

        // Act
        var highConfidenceAtoms = _atomSpace.Query().WithMinConfidence(0.5).Execute().ToList();

        // Assert
        Assert.Contains(highConfidenceNode, highConfidenceAtoms);
        Assert.DoesNotContain(lowConfidenceNode, highConfidenceAtoms);
    }

    [Fact]
    public void Query_ChainedFilters_WorkCorrectly()
    {
        // Act
        var results = _atomSpace.Query()
            .OfType(AtomType.Node)
            .AtScale(ScaleLevel.Cellular)
            .WithNameContaining("Test")
            .Execute()
            .ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal("TestNode", ((Node)results[0]).Name);
        Assert.Equal(ScaleLevel.Cellular, results[0].Scale);
        Assert.Equal(AtomType.Node, results[0].Type);
    }

    [Fact]
    public void Query_CustomWhere_FiltersCorrectly()
    {
        // Act
        var results = _atomSpace.Query()
            .Where(atom => atom.Id.Contains("skin"))
            .Execute()
            .ToList();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, atom => Assert.Contains("skin", atom.Id));
    }

    [Fact]
    public void Query_ExecuteAsNodes_ReturnsOnlyNodes()
    {
        // Act
        var nodes = _atomSpace.Query().ExecuteAsNodes().ToList();

        // Assert
        Assert.Equal(5, nodes.Count); // 3 regular nodes + 2 skin nodes
        Assert.All(nodes, node => Assert.True(node is Node)); // SkinNode is also a Node
    }

    [Fact]
    public void Query_ExecuteAsLinks_ReturnsOnlyLinks()
    {
        // Act
        var links = _atomSpace.Query().ExecuteAsLinks().ToList();

        // Assert
        Assert.Equal(2, links.Count);
        Assert.All(links, link => Assert.IsType<Link>(link));
    }

    [Fact]
    public void Query_ExecuteAsSkinNodes_ReturnsOnlySkinNodes()
    {
        // Act
        var skinNodes = _atomSpace.Query().ExecuteAsSkinNodes().ToList();

        // Assert
        Assert.Equal(2, skinNodes.Count);
        Assert.All(skinNodes, skinNode => Assert.IsType<SkinNode>(skinNode));
    }

    [Fact]
    public void FindConnectedAtoms_FindsConnections()
    {
        // Act
        var connectedToNode1 = _atomSpace.FindConnectedAtoms("node1", 1).ToList();
        var connectedToSkin1 = _atomSpace.FindConnectedAtoms("skin1", 1).ToList();

        // Assert
        // node1 should be connected to link1, and link1 connects to node2
        Assert.Contains(_atomSpace.GetAtom("link1"), connectedToNode1); // The incoming link
        // skin1 should be connected to link2, and link2 connects to skin2  
        Assert.Contains(_atomSpace.GetAtom("link2"), connectedToSkin1); // The incoming link
        
        // Check if we can find the other nodes through the links at depth 1
        var connectedToNode1Depth2 = _atomSpace.FindConnectedAtoms("node1", 2).ToList();
        var connectedToSkin1Depth2 = _atomSpace.FindConnectedAtoms("skin1", 2).ToList();
        
        Assert.Contains(_atomSpace.GetAtom("node2"), connectedToNode1Depth2); // Connected via link1 at depth 2
        Assert.Contains(_atomSpace.GetAtom("skin2"), connectedToSkin1Depth2); // Connected via link2 at depth 2
    }

    [Fact]
    public void FindScaleBridges_FindsBridgeLinks()
    {
        // Act
        var bridges = _atomSpace.FindScaleBridges(ScaleLevel.Cellular, ScaleLevel.Molecular).ToList();

        // Assert
        Assert.Single(bridges);
        Assert.Equal("link2", bridges[0].Id); // link2 connects cellular (skin1) to molecular (skin2)
    }

    [Fact]
    public void FindConnectedAtoms_WithMaxDepth_LimitsDepth()
    {
        // Arrange
        var node4 = new Node("node4", "Node4", ScaleLevel.Tissue);
        var link3 = new Link("link3", "Connects", ScaleLevel.Tissue, new[] { _atomSpace.GetAtom("node2")!, node4 });
        _atomSpace.AddAtom(node4);
        _atomSpace.AddAtom(link3);

        // Act
        var depth1 = _atomSpace.FindConnectedAtoms("node1", 1).ToList();
        var depth2 = _atomSpace.FindConnectedAtoms("node1", 2).ToList();
        var depth4 = _atomSpace.FindConnectedAtoms("node1", 4).ToList();

        // Assert
        Assert.True(depth2.Count >= depth1.Count);
        Assert.True(depth4.Count >= depth2.Count);
        Assert.Contains(node4, depth4); // Should reach node4 at depth 4: node1->link1->node2->link3->node4
        
        // Verify depth limiting works
        Assert.DoesNotContain(node4, depth1); // node4 should not be reachable at depth 1
        Assert.DoesNotContain(node4, depth2); // node4 should not be reachable at depth 2
    }
}