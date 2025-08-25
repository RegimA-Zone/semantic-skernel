// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Linq;
using Microsoft.SemanticKernel.Plugins.AtomSpace;
using Xunit;

namespace SemanticKernel.Plugins.UnitTests.AtomSpace;

public class AtomsTests
{
    [Fact]
    public void Node_ValidConstruction_SetsProperties()
    {
        // Arrange
        var id = "node-1";
        var name = "TestNode";
        var scale = ScaleLevel.Cellular;
        var data = "test data";
        var truthValue = new TruthValue(0.8, 0.9);

        // Act
        var node = new Node(id, name, scale, data, truthValue);

        // Assert
        Assert.Equal(id, node.Id);
        Assert.Equal(name, node.Name);
        Assert.Equal(scale, node.Scale);
        Assert.Equal(data, node.Data);
        Assert.Equal(AtomType.Node, node.Type);
        Assert.Equal(truthValue, node.TruthValue);
    }

    [Fact]
    public void Node_NullName_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Node("id", null!, ScaleLevel.Cellular));
    }

    [Fact]
    public void Link_ValidConstruction_SetsProperties()
    {
        // Arrange
        var id = "link-1";
        var linkType = "TestLink";
        var scale = ScaleLevel.Tissue;
        var node1 = new Node("node1", "Node1", ScaleLevel.Cellular);
        var node2 = new Node("node2", "Node2", ScaleLevel.Cellular);
        var outgoingSet = new[] { node1, node2 };
        var truthValue = new TruthValue(0.7, 0.8);

        // Act
        var link = new Link(id, linkType, scale, outgoingSet, truthValue);

        // Assert
        Assert.Equal(id, link.Id);
        Assert.Equal(linkType, link.LinkType);
        Assert.Equal(scale, link.Scale);
        Assert.Equal(AtomType.Link, link.Type);
        Assert.Equal(2, link.OutgoingSet.Count);
        Assert.Contains(node1, link.OutgoingSet);
        Assert.Contains(node2, link.OutgoingSet);
        Assert.Equal(truthValue, link.TruthValue);
    }

    [Fact]
    public void Link_EmptyOutgoingSet_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Link("id", "type", ScaleLevel.Cellular, Array.Empty<Atom>()));
    }

    [Fact]
    public void Link_NullOutgoingSet_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Link("id", "type", ScaleLevel.Cellular, null!));
    }

    [Fact]
    public void SkinNode_ValidConstruction_SetsProperties()
    {
        // Arrange
        var id = "skin-node-1";
        var name = "Keratinocyte";
        var scale = ScaleLevel.Cellular;
        var componentType = SkinComponentType.Keratinocyte;
        var data = "cell data";
        var truthValue = new TruthValue(0.9, 0.95);

        // Act
        var skinNode = new SkinNode(id, name, scale, componentType, data, truthValue);

        // Assert
        Assert.Equal(id, skinNode.Id);
        Assert.Equal(name, skinNode.Name);
        Assert.Equal(scale, skinNode.Scale);
        Assert.Equal(componentType, skinNode.ComponentType);
        Assert.Equal(data, skinNode.Data);
        Assert.Equal(AtomType.Node, skinNode.Type);
        Assert.Equal(truthValue, skinNode.TruthValue);
    }

    [Fact]
    public void SkinComponentType_HasExpectedValues()
    {
        // Assert molecular level components
        Assert.True(Enum.IsDefined(typeof(SkinComponentType), SkinComponentType.Protein));
        Assert.True(Enum.IsDefined(typeof(SkinComponentType), SkinComponentType.Lipid));
        Assert.True(Enum.IsDefined(typeof(SkinComponentType), SkinComponentType.DNA));
        Assert.True(Enum.IsDefined(typeof(SkinComponentType), SkinComponentType.RNA));
        Assert.True(Enum.IsDefined(typeof(SkinComponentType), SkinComponentType.Enzyme));

        // Assert cellular level components
        Assert.True(Enum.IsDefined(typeof(SkinComponentType), SkinComponentType.Keratinocyte));
        Assert.True(Enum.IsDefined(typeof(SkinComponentType), SkinComponentType.Melanocyte));
        Assert.True(Enum.IsDefined(typeof(SkinComponentType), SkinComponentType.LangerhansCell));
        Assert.True(Enum.IsDefined(typeof(SkinComponentType), SkinComponentType.Fibroblast));

        // Assert tissue level components
        Assert.True(Enum.IsDefined(typeof(SkinComponentType), SkinComponentType.Epidermis));
        Assert.True(Enum.IsDefined(typeof(SkinComponentType), SkinComponentType.Dermis));
        Assert.True(Enum.IsDefined(typeof(SkinComponentType), SkinComponentType.Hypodermis));
        Assert.True(Enum.IsDefined(typeof(SkinComponentType), SkinComponentType.HairFollicle));

        // Assert organ level components
        Assert.True(Enum.IsDefined(typeof(SkinComponentType), SkinComponentType.SkinRegion));
        Assert.True(Enum.IsDefined(typeof(SkinComponentType), SkinComponentType.SkinSystem));
    }

    [Fact]
    public void Node_ToString_ReturnsExpectedFormat()
    {
        // Arrange
        var node = new Node("id", "TestNode", ScaleLevel.Cellular);

        // Act
        var result = node.ToString();

        // Assert
        Assert.Equal("Node(TestNode, Scale: Cellular)", result);
    }

    [Fact]
    public void Link_ToString_ReturnsExpectedFormat()
    {
        // Arrange
        var node1 = new Node("node1", "Node1", ScaleLevel.Cellular);
        var node2 = new Node("node2", "Node2", ScaleLevel.Cellular);
        var link = new Link("id", "TestLink", ScaleLevel.Tissue, new[] { node1, node2 });

        // Act
        var result = link.ToString();

        // Assert
        Assert.Equal("Link(TestLink, Scale: Tissue, Arity: 2)", result);
    }

    [Fact]
    public void SkinNode_ToString_ReturnsExpectedFormat()
    {
        // Arrange
        var skinNode = new SkinNode("id", "Keratinocyte", ScaleLevel.Cellular, SkinComponentType.Keratinocyte);

        // Act
        var result = skinNode.ToString();

        // Assert
        Assert.Equal("SkinNode(Keratinocyte, Scale: Cellular, Type: Keratinocyte)", result);
    }
}