// Copyright (c) Microsoft. All rights reserved.

using System.Linq;
using Microsoft.SemanticKernel.Plugins.AtomSpace;
using Xunit;

namespace SemanticKernel.Plugins.UnitTests.AtomSpace;

public class AtomSpaceTests
{
    [Fact]
    public void AtomSpace_DefaultConstruction_InitializesCorrectly()
    {
        // Act
        var atomSpace = new Microsoft.SemanticKernel.Plugins.AtomSpace.AtomSpace();

        // Assert
        Assert.NotNull(atomSpace.Configuration);
        Assert.Equal(0, atomSpace.Count);
        Assert.Empty(atomSpace.Atoms);
    }

    [Fact]
    public void AtomSpace_WithConfiguration_InitializesCorrectly()
    {
        // Arrange
        var config = new AtomSpaceConfiguration
        {
            MaxAtoms = 50000,
            EnableAutomaticGarbageCollection = true
        };

        // Act
        var atomSpace = new Microsoft.SemanticKernel.Plugins.AtomSpace.AtomSpace(config);

        // Assert
        Assert.Equal(config, atomSpace.Configuration);
        Assert.Equal(50000, atomSpace.Configuration.MaxAtoms);
        Assert.True(atomSpace.Configuration.EnableAutomaticGarbageCollection);
    }

    [Fact]
    public void AddAtom_NewAtom_AddsSuccessfully()
    {
        // Arrange
        var atomSpace = new Microsoft.SemanticKernel.Plugins.AtomSpace.AtomSpace();
        var node = new Node("node1", "TestNode", ScaleLevel.Cellular);

        // Act
        var addedAtom = atomSpace.AddAtom(node);

        // Assert
        Assert.Equal(node, addedAtom);
        Assert.Equal(1, atomSpace.Count);
        Assert.Contains(node, atomSpace.Atoms);
    }

    [Fact]
    public void AddAtom_DuplicateId_ReturnsExisting()
    {
        // Arrange
        var atomSpace = new Microsoft.SemanticKernel.Plugins.AtomSpace.AtomSpace();
        var node1 = new Node("node1", "TestNode1", ScaleLevel.Cellular, null, new TruthValue(0.5, 0.6));
        var node2 = new Node("node1", "TestNode2", ScaleLevel.Molecular, null, new TruthValue(0.8, 0.9)); // Same ID, different properties

        // Act
        var addedAtom1 = atomSpace.AddAtom(node1);
        var addedAtom2 = atomSpace.AddAtom(node2);

        // Assert
        Assert.Equal(node1, addedAtom1);
        Assert.Equal(node1, addedAtom2); // Returns existing atom
        Assert.Equal(1, atomSpace.Count);
        Assert.Equal(0.8, addedAtom1.TruthValue.Strength); // Truth value updated to higher confidence
        Assert.Equal(0.9, addedAtom1.TruthValue.Confidence);
    }

    [Fact]
    public void GetAtom_ExistingId_ReturnsAtom()
    {
        // Arrange
        var atomSpace = new Microsoft.SemanticKernel.Plugins.AtomSpace.AtomSpace();
        var node = new Node("node1", "TestNode", ScaleLevel.Cellular);
        atomSpace.AddAtom(node);

        // Act
        var retrievedAtom = atomSpace.GetAtom("node1");

        // Assert
        Assert.Equal(node, retrievedAtom);
    }

    [Fact]
    public void GetAtom_NonExistingId_ReturnsNull()
    {
        // Arrange
        var atomSpace = new Microsoft.SemanticKernel.Plugins.AtomSpace.AtomSpace();

        // Act
        var retrievedAtom = atomSpace.GetAtom("nonexistent");

        // Assert
        Assert.Null(retrievedAtom);
    }

    [Fact]
    public void RemoveAtom_ExistingAtom_RemovesSuccessfully()
    {
        // Arrange
        var atomSpace = new Microsoft.SemanticKernel.Plugins.AtomSpace.AtomSpace();
        var node = new Node("node1", "TestNode", ScaleLevel.Cellular);
        atomSpace.AddAtom(node);

        // Act
        var removed = atomSpace.RemoveAtom("node1");

        // Assert
        Assert.True(removed);
        Assert.Equal(0, atomSpace.Count);
        Assert.DoesNotContain(node, atomSpace.Atoms);
    }

    [Fact]
    public void RemoveAtom_NonExistingAtom_ReturnsFalse()
    {
        // Arrange
        var atomSpace = new Microsoft.SemanticKernel.Plugins.AtomSpace.AtomSpace();

        // Act
        var removed = atomSpace.RemoveAtom("nonexistent");

        // Assert
        Assert.False(removed);
    }

    [Fact]
    public void AddLink_CreatesIncomingReferences()
    {
        // Arrange
        var atomSpace = new Microsoft.SemanticKernel.Plugins.AtomSpace.AtomSpace();
        var node1 = new Node("node1", "Node1", ScaleLevel.Cellular);
        var node2 = new Node("node2", "Node2", ScaleLevel.Cellular);
        var link = new Link("link1", "TestLink", ScaleLevel.Tissue, new[] { node1, node2 });

        // Act
        atomSpace.AddAtom(node1);
        atomSpace.AddAtom(node2);
        atomSpace.AddAtom(link);

        // Assert
        var incomingLinks1 = atomSpace.GetIncomingLinks("node1").ToList();
        var incomingLinks2 = atomSpace.GetIncomingLinks("node2").ToList();

        Assert.Single(incomingLinks1);
        Assert.Single(incomingLinks2);
        Assert.Equal(link, incomingLinks1[0]);
        Assert.Equal(link, incomingLinks2[0]);
    }

    [Fact]
    public void GetAtomsByType_FiltersByType()
    {
        // Arrange
        var atomSpace = new Microsoft.SemanticKernel.Plugins.AtomSpace.AtomSpace();
        var node1 = new Node("node1", "Node1", ScaleLevel.Cellular);
        var node2 = new Node("node2", "Node2", ScaleLevel.Molecular);
        var link = new Link("link1", "TestLink", ScaleLevel.Tissue, new[] { node1 });

        atomSpace.AddAtom(node1);
        atomSpace.AddAtom(node2);
        atomSpace.AddAtom(link);

        // Act
        var nodes = atomSpace.GetAtomsByType(AtomType.Node).ToList();
        var links = atomSpace.GetAtomsByType(AtomType.Link).ToList();

        // Assert
        Assert.Equal(2, nodes.Count);
        Assert.Single(links);
        Assert.Contains(node1, nodes);
        Assert.Contains(node2, nodes);
        Assert.Contains(link, links);
    }

    [Fact]
    public void GetAtomsByScale_FiltersByScale()
    {
        // Arrange
        var atomSpace = new Microsoft.SemanticKernel.Plugins.AtomSpace.AtomSpace();
        var node1 = new Node("node1", "Node1", ScaleLevel.Cellular);
        var node2 = new Node("node2", "Node2", ScaleLevel.Molecular);
        var node3 = new Node("node3", "Node3", ScaleLevel.Cellular);

        atomSpace.AddAtom(node1);
        atomSpace.AddAtom(node2);
        atomSpace.AddAtom(node3);

        // Act
        var cellularAtoms = atomSpace.GetAtomsByScale(ScaleLevel.Cellular).ToList();
        var molecularAtoms = atomSpace.GetAtomsByScale(ScaleLevel.Molecular).ToList();

        // Assert
        Assert.Equal(2, cellularAtoms.Count);
        Assert.Single(molecularAtoms);
        Assert.Contains(node1, cellularAtoms);
        Assert.Contains(node3, cellularAtoms);
        Assert.Contains(node2, molecularAtoms);
    }

    [Fact]
    public void Clear_RemovesAllAtoms()
    {
        // Arrange
        var atomSpace = new Microsoft.SemanticKernel.Plugins.AtomSpace.AtomSpace();
        var node1 = new Node("node1", "Node1", ScaleLevel.Cellular);
        var node2 = new Node("node2", "Node2", ScaleLevel.Molecular);

        atomSpace.AddAtom(node1);
        atomSpace.AddAtom(node2);

        // Act
        atomSpace.Clear();

        // Assert
        Assert.Equal(0, atomSpace.Count);
        Assert.Empty(atomSpace.Atoms);
    }

    [Fact]
    public void Query_ReturnsQueryBuilder()
    {
        // Arrange
        var atomSpace = new Microsoft.SemanticKernel.Plugins.AtomSpace.AtomSpace();

        // Act
        var query = atomSpace.Query();

        // Assert
        Assert.NotNull(query);
        Assert.IsType<AtomSpaceQuery>(query);
    }

    [Fact]
    public void Configuration_DefaultValues_SetCorrectly()
    {
        // Arrange & Act
        var config = new AtomSpaceConfiguration();

        // Assert
        Assert.Equal(100000, config.MaxAtoms);
        Assert.False(config.EnableAutomaticGarbageCollection);
        Assert.Equal(0.1, config.MinimumConfidenceThreshold);
        Assert.NotNull(config.SkinConfiguration);
    }

    [Fact]
    public void MultiscaleSkinConfiguration_DefaultValues_SetCorrectly()
    {
        // Arrange & Act
        var skinConfig = new MultiscaleSkinConfiguration();

        // Assert
        Assert.Equal(4, skinConfig.EnabledScales.Count);
        Assert.Contains(ScaleLevel.Molecular, skinConfig.EnabledScales);
        Assert.Contains(ScaleLevel.Cellular, skinConfig.EnabledScales);
        Assert.Contains(ScaleLevel.Tissue, skinConfig.EnabledScales);
        Assert.Contains(ScaleLevel.Organ, skinConfig.EnabledScales);
        Assert.NotNull(skinConfig.ScaleTransitionWeights);
        Assert.NotNull(skinConfig.DefaultTruthValues);
    }
}