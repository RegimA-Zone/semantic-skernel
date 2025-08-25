// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.SemanticKernel.Plugins.AtomSpace;
using Xunit;

namespace SemanticKernel.Plugins.UnitTests.AtomSpace;

public class AtomTypesTests
{
    [Fact]
    public void TruthValue_ValidConstruction_SetsProperties()
    {
        // Arrange
        var strength = 0.8;
        var confidence = 0.9;

        // Act
        var truthValue = new TruthValue(strength, confidence);

        // Assert
        Assert.Equal(strength, truthValue.Strength);
        Assert.Equal(confidence, truthValue.Confidence);
    }

    [Theory]
    [InlineData(-0.1, 0.5)]
    [InlineData(1.1, 0.5)]
    [InlineData(0.5, -0.1)]
    [InlineData(0.5, 1.1)]
    public void TruthValue_InvalidValues_ThrowsArgumentOutOfRangeException(double strength, double confidence)
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new TruthValue(strength, confidence));
    }

    [Fact]
    public void Atom_Equality_WorksCorrectly()
    {
        // Arrange
        var atom1 = new TestAtom("test-id", AtomType.Node, ScaleLevel.Cellular);
        var atom2 = new TestAtom("test-id", AtomType.Link, ScaleLevel.Molecular); // Different type and scale but same ID
        var atom3 = new TestAtom("different-id", AtomType.Node, ScaleLevel.Cellular);

        // Act & Assert
        Assert.Equal(atom1, atom2); // Same ID
        Assert.NotEqual(atom1, atom3); // Different ID
        Assert.Equal(atom1.GetHashCode(), atom2.GetHashCode()); // Same ID
    }

    [Fact]
    public void Atom_HasDefaultTruthValue()
    {
        // Arrange & Act
        var atom = new TestAtom("test-id", AtomType.Node, ScaleLevel.Cellular);

        // Assert
        Assert.Equal(1.0, atom.TruthValue.Strength);
        Assert.Equal(1.0, atom.TruthValue.Confidence);
    }

    [Fact]
    public void Atom_HasEmptyMetadata()
    {
        // Arrange & Act
        var atom = new TestAtom("test-id", AtomType.Node, ScaleLevel.Cellular);

        // Assert
        Assert.NotNull(atom.Metadata);
        Assert.Empty(atom.Metadata);
    }

    [Fact]
    public void ScaleLevel_HasCorrectValues()
    {
        // Assert
        Assert.Equal(0, (int)ScaleLevel.Molecular);
        Assert.Equal(1, (int)ScaleLevel.Cellular);
        Assert.Equal(2, (int)ScaleLevel.Tissue);
        Assert.Equal(3, (int)ScaleLevel.Organ);
    }

    // Test implementation of Atom for testing purposes
    private class TestAtom : Atom
    {
        public TestAtom(string id, AtomType type, ScaleLevel scale, TruthValue? truthValue = null)
            : base(id, type, scale, truthValue)
        {
        }
    }
}