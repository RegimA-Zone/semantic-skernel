// Copyright (c) Microsoft. All rights reserved.

using System.Linq;
using Microsoft.SemanticKernel.Plugins.AtomSpace;
using Xunit;

namespace SemanticKernel.Plugins.UnitTests.AtomSpace;

public class MultiscaleSkinModelBuilderTests
{
    [Fact]
    public void MultiscaleSkinModelBuilder_ValidConstruction()
    {
        // Arrange
        var atomSpace = new Microsoft.SemanticKernel.Plugins.AtomSpace.AtomSpace();

        // Act
        var builder = new MultiscaleSkinModelBuilder(atomSpace);

        // Assert
        Assert.NotNull(builder);
    }

    [Fact]
    public void CreateBasicSkinModel_CreatesExpectedComponents()
    {
        // Arrange
        var atomSpace = new Microsoft.SemanticKernel.Plugins.AtomSpace.AtomSpace();
        var builder = new MultiscaleSkinModelBuilder(atomSpace);

        // Act
        var createdAtoms = builder.CreateBasicSkinModel();

        // Assert
        Assert.True(createdAtoms.Count > 0);
        
        // Verify molecular level components
        var molecularComponents = createdAtoms.OfType<SkinNode>()
            .Where(n => n.Scale == ScaleLevel.Molecular)
            .ToList();
        Assert.Contains(molecularComponents, n => n.ComponentType == SkinComponentType.Protein);
        Assert.Contains(molecularComponents, n => n.ComponentType == SkinComponentType.Lipid);

        // Verify cellular level components
        var cellularComponents = createdAtoms.OfType<SkinNode>()
            .Where(n => n.Scale == ScaleLevel.Cellular)
            .ToList();
        Assert.Contains(cellularComponents, n => n.ComponentType == SkinComponentType.Keratinocyte);
        Assert.Contains(cellularComponents, n => n.ComponentType == SkinComponentType.Melanocyte);
        Assert.Contains(cellularComponents, n => n.ComponentType == SkinComponentType.Fibroblast);

        // Verify tissue level components
        var tissueComponents = createdAtoms.OfType<SkinNode>()
            .Where(n => n.Scale == ScaleLevel.Tissue)
            .ToList();
        Assert.Contains(tissueComponents, n => n.ComponentType == SkinComponentType.Epidermis);
        Assert.Contains(tissueComponents, n => n.ComponentType == SkinComponentType.Dermis);
        Assert.Contains(tissueComponents, n => n.ComponentType == SkinComponentType.Hypodermis);

        // Verify organ level components
        var organComponents = createdAtoms.OfType<SkinNode>()
            .Where(n => n.Scale == ScaleLevel.Organ)
            .ToList();
        Assert.Contains(organComponents, n => n.ComponentType == SkinComponentType.SkinSystem);
    }

    [Fact]
    public void CreateBasicSkinModel_CreatesRelationships()
    {
        // Arrange
        var atomSpace = new Microsoft.SemanticKernel.Plugins.AtomSpace.AtomSpace();
        var builder = new MultiscaleSkinModelBuilder(atomSpace);

        // Act
        var createdAtoms = builder.CreateBasicSkinModel();

        // Assert
        var links = createdAtoms.OfType<Link>().ToList();
        Assert.True(links.Count > 0);

        // Verify different types of relationships exist
        var linkTypes = links.Select(l => l.LinkType).Distinct().ToList();
        Assert.Contains("Contains", linkTypes);
        Assert.Contains("ComprisesOf", linkTypes);
        Assert.Contains("PartOf", linkTypes);
        Assert.Contains("Protects", linkTypes);
    }

    [Fact]
    public void CreateBasicSkinModel_AllAtomsAddedToAtomSpace()
    {
        // Arrange
        var atomSpace = new Microsoft.SemanticKernel.Plugins.AtomSpace.AtomSpace();
        var builder = new MultiscaleSkinModelBuilder(atomSpace);

        // Act
        var createdAtoms = builder.CreateBasicSkinModel();

        // Assert
        Assert.Equal(createdAtoms.Count, atomSpace.Count);
        foreach (var atom in createdAtoms)
        {
            Assert.Equal(atom, atomSpace.GetAtom(atom.Id));
        }
    }

    [Fact]
    public void SkinModelCustomization_DefaultValues()
    {
        // Act
        var customization = new SkinModelCustomization();

        // Assert
        Assert.Equal(4, customization.EnabledScales.Count);
        Assert.Contains(ScaleLevel.Molecular, customization.EnabledScales);
        Assert.Contains(ScaleLevel.Cellular, customization.EnabledScales);
        Assert.Contains(ScaleLevel.Tissue, customization.EnabledScales);
        Assert.Contains(ScaleLevel.Organ, customization.EnabledScales);
        Assert.NotNull(customization.CustomComponents);
        Assert.NotNull(customization.CustomRelationships);
        Assert.NotNull(customization.CustomTruthValues);
    }

    [Fact]
    public void SkinModelFactory_CreateCustomizedSkinModel_WithCustomComponents()
    {
        // Arrange
        var atomSpace = new Microsoft.SemanticKernel.Plugins.AtomSpace.AtomSpace();
        var customization = new SkinModelCustomization();
        customization.CustomComponents[ScaleLevel.Molecular] = new()
        {
            ("CustomProtein", SkinComponentType.Protein, "custom data")
        };
        customization.CustomTruthValues["CustomProtein"] = new TruthValue(0.95, 0.99);

        // Act
        var createdAtoms = SkinModelFactory.CreateCustomizedSkinModel(atomSpace, customization);

        // Assert
        var customProtein = createdAtoms.OfType<SkinNode>()
            .FirstOrDefault(n => n.Name == "CustomProtein");
        Assert.NotNull(customProtein);
        Assert.Equal(SkinComponentType.Protein, customProtein.ComponentType);
        Assert.Equal(ScaleLevel.Molecular, customProtein.Scale);
        Assert.Equal("custom data", customProtein.Data);
        Assert.Equal(0.95, customProtein.TruthValue.Strength);
        Assert.Equal(0.99, customProtein.TruthValue.Confidence);
    }

    [Fact]
    public void SkinModelFactory_CreateCustomizedSkinModel_WithLimitedScales()
    {
        // Arrange
        var atomSpace = new Microsoft.SemanticKernel.Plugins.AtomSpace.AtomSpace();
        var customization = new SkinModelCustomization();
        customization.EnabledScales.Clear();
        customization.EnabledScales.Add(ScaleLevel.Cellular);
        customization.EnabledScales.Add(ScaleLevel.Tissue);

        customization.CustomComponents[ScaleLevel.Cellular] = new()
        {
            ("TestCell", SkinComponentType.Keratinocyte, null)
        };
        customization.CustomComponents[ScaleLevel.Molecular] = new()  // This should be ignored
        {
            ("TestMolecule", SkinComponentType.Protein, null)
        };

        // Act
        var createdAtoms = SkinModelFactory.CreateCustomizedSkinModel(atomSpace, customization);

        // Assert
        var testCell = createdAtoms.OfType<SkinNode>()
            .FirstOrDefault(n => n.Name == "TestCell");
        var testMolecule = createdAtoms.OfType<SkinNode>()
            .FirstOrDefault(n => n.Name == "TestMolecule");

        Assert.NotNull(testCell);
        Assert.Null(testMolecule); // Should not be created because Molecular scale is not enabled
    }

    [Fact]
    public void SkinModelFactory_CreateCustomizedSkinModel_WithCustomRelationships()
    {
        // Arrange
        var atomSpace = new Microsoft.SemanticKernel.Plugins.AtomSpace.AtomSpace();
        var customization = new SkinModelCustomization();
        customization.CustomComponents[ScaleLevel.Cellular] = new()
        {
            ("CellA", SkinComponentType.Keratinocyte, null),
            ("CellB", SkinComponentType.Melanocyte, null)
        };
        customization.CustomRelationships.Add(("CustomInteracts", "CellA", "CellB", ScaleLevel.Cellular));

        // Act
        var createdAtoms = SkinModelFactory.CreateCustomizedSkinModel(atomSpace, customization);

        // Assert
        var customLink = createdAtoms.OfType<Link>()
            .FirstOrDefault(l => l.LinkType == "CustomInteracts");
        Assert.NotNull(customLink);
        Assert.Equal(ScaleLevel.Cellular, customLink.Scale);
        Assert.Equal(2, customLink.OutgoingSet.Count);
        
        var cellA = customLink.OutgoingSet.OfType<SkinNode>().FirstOrDefault(n => n.Name == "CellA");
        var cellB = customLink.OutgoingSet.OfType<SkinNode>().FirstOrDefault(n => n.Name == "CellB");
        Assert.NotNull(cellA);
        Assert.NotNull(cellB);
    }

    [Fact]
    public void CreateBasicSkinModel_ComponentsHaveAppropriateScales()
    {
        // Arrange
        var atomSpace = new Microsoft.SemanticKernel.Plugins.AtomSpace.AtomSpace();
        var builder = new MultiscaleSkinModelBuilder(atomSpace);

        // Act
        var createdAtoms = builder.CreateBasicSkinModel();

        // Assert
        var skinNodes = createdAtoms.OfType<SkinNode>().ToList();

        // Verify molecular components are at molecular scale
        var proteinNodes = skinNodes.Where(n => n.ComponentType == SkinComponentType.Protein);
        Assert.All(proteinNodes, n => Assert.Equal(ScaleLevel.Molecular, n.Scale));

        // Verify cellular components are at cellular scale
        var cellNodes = skinNodes.Where(n => n.ComponentType == SkinComponentType.Keratinocyte ||
                                            n.ComponentType == SkinComponentType.Melanocyte ||
                                            n.ComponentType == SkinComponentType.Fibroblast);
        Assert.All(cellNodes, n => Assert.Equal(ScaleLevel.Cellular, n.Scale));

        // Verify tissue components are at tissue scale
        var tissueNodes = skinNodes.Where(n => n.ComponentType == SkinComponentType.Epidermis ||
                                              n.ComponentType == SkinComponentType.Dermis ||
                                              n.ComponentType == SkinComponentType.Hypodermis);
        Assert.All(tissueNodes, n => Assert.Equal(ScaleLevel.Tissue, n.Scale));

        // Verify organ components are at organ scale
        var organNodes = skinNodes.Where(n => n.ComponentType == SkinComponentType.SkinSystem);
        Assert.All(organNodes, n => Assert.Equal(ScaleLevel.Organ, n.Scale));
    }
}