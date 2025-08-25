// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.SemanticKernel.Plugins.AtomSpace;

/// <summary>
/// Builder for creating multiscale skin models in the atomspace.
/// </summary>
public class MultiscaleSkinModelBuilder
{
    private readonly AtomSpace _atomSpace;

    public MultiscaleSkinModelBuilder(AtomSpace atomSpace)
    {
        _atomSpace = atomSpace ?? throw new ArgumentNullException(nameof(atomSpace));
    }

    /// <summary>
    /// Creates a basic multiscale skin model with predefined components and relationships.
    /// </summary>
    /// <returns>A collection of created atoms representing the skin model.</returns>
    public ICollection<Atom> CreateBasicSkinModel()
    {
        var createdAtoms = new List<Atom>();

        // Molecular level components
        var collagen = CreateSkinNode("Collagen", ScaleLevel.Molecular, SkinComponentType.Protein);
        var elastin = CreateSkinNode("Elastin", ScaleLevel.Molecular, SkinComponentType.Protein);
        var keratin = CreateSkinNode("Keratin", ScaleLevel.Molecular, SkinComponentType.Protein);
        var ceramides = CreateSkinNode("Ceramides", ScaleLevel.Molecular, SkinComponentType.Lipid);
        var melanin = CreateSkinNode("Melanin", ScaleLevel.Molecular, SkinComponentType.Protein);

        createdAtoms.AddRange(new[] { collagen, elastin, keratin, ceramides, melanin });

        // Cellular level components
        var keratinocyte = CreateSkinNode("Keratinocyte", ScaleLevel.Cellular, SkinComponentType.Keratinocyte);
        var melanocyte = CreateSkinNode("Melanocyte", ScaleLevel.Cellular, SkinComponentType.Melanocyte);
        var fibroblast = CreateSkinNode("Fibroblast", ScaleLevel.Cellular, SkinComponentType.Fibroblast);
        var langerhansCell = CreateSkinNode("LangerhansCell", ScaleLevel.Cellular, SkinComponentType.LangerhansCell);

        createdAtoms.AddRange(new[] { keratinocyte, melanocyte, fibroblast, langerhansCell });

        // Tissue level components
        var epidermis = CreateSkinNode("Epidermis", ScaleLevel.Tissue, SkinComponentType.Epidermis);
        var dermis = CreateSkinNode("Dermis", ScaleLevel.Tissue, SkinComponentType.Dermis);
        var hypodermis = CreateSkinNode("Hypodermis", ScaleLevel.Tissue, SkinComponentType.Hypodermis);
        var hairFollicle = CreateSkinNode("HairFollicle", ScaleLevel.Tissue, SkinComponentType.HairFollicle);

        createdAtoms.AddRange(new[] { epidermis, dermis, hypodermis, hairFollicle });

        // Organ level
        var skinSystem = CreateSkinNode("SkinSystem", ScaleLevel.Organ, SkinComponentType.SkinSystem);
        createdAtoms.Add(skinSystem);

        // Create relationships between scales
        CreateScaleRelationships(createdAtoms);

        return createdAtoms;
    }

    /// <summary>
    /// Creates relationships between components across different scales.
    /// </summary>
    /// <param name="atoms">The atoms to create relationships between.</param>
    private void CreateScaleRelationships(ICollection<Atom> atoms)
    {
        var atomsDict = atoms.OfType<SkinNode>().ToDictionary(n => n.Name, n => n);

        // Molecular to Cellular relationships
        CreateLink("Contains", ScaleLevel.Cellular, atomsDict["Keratinocyte"], atomsDict["Keratin"]);
        CreateLink("Produces", ScaleLevel.Cellular, atomsDict["Melanocyte"], atomsDict["Melanin"]);
        CreateLink("Synthesizes", ScaleLevel.Cellular, atomsDict["Fibroblast"], atomsDict["Collagen"]);
        CreateLink("Synthesizes", ScaleLevel.Cellular, atomsDict["Fibroblast"], atomsDict["Elastin"]);

        // Cellular to Tissue relationships
        CreateLink("ComprisesOf", ScaleLevel.Tissue, atomsDict["Epidermis"], atomsDict["Keratinocyte"]);
        CreateLink("ComprisesOf", ScaleLevel.Tissue, atomsDict["Epidermis"], atomsDict["Melanocyte"]);
        CreateLink("ComprisesOf", ScaleLevel.Tissue, atomsDict["Epidermis"], atomsDict["LangerhansCell"]);
        CreateLink("ComprisesOf", ScaleLevel.Tissue, atomsDict["Dermis"], atomsDict["Fibroblast"]);

        // Tissue to Organ relationships
        CreateLink("PartOf", ScaleLevel.Organ, atomsDict["SkinSystem"], atomsDict["Epidermis"]);
        CreateLink("PartOf", ScaleLevel.Organ, atomsDict["SkinSystem"], atomsDict["Dermis"]);
        CreateLink("PartOf", ScaleLevel.Organ, atomsDict["SkinSystem"], atomsDict["Hypodermis"]);
        CreateLink("PartOf", ScaleLevel.Organ, atomsDict["SkinSystem"], atomsDict["HairFollicle"]);

        // Cross-scale functional relationships
        CreateLink("Protects", ScaleLevel.Tissue, atomsDict["Epidermis"], atomsDict["Dermis"]);
        CreateLink("Supports", ScaleLevel.Tissue, atomsDict["Dermis"], atomsDict["Epidermis"]);
        CreateLink("Insulates", ScaleLevel.Tissue, atomsDict["Hypodermis"], atomsDict["Dermis"]);
    }

    /// <summary>
    /// Creates a skin node and adds it to the atomspace.
    /// </summary>
    /// <param name="name">The name of the component.</param>
    /// <param name="scale">The scale level.</param>
    /// <param name="componentType">The skin component type.</param>
    /// <returns>The created skin node.</returns>
    private SkinNode CreateSkinNode(string name, ScaleLevel scale, SkinComponentType componentType)
    {
        var atomId = $"skin_{scale}_{name}_{Guid.NewGuid():N}";
        var truthValue = GetDefaultTruthValueForComponent(componentType);
        var skinNode = new SkinNode(atomId, name, scale, componentType, null, truthValue);
        return (SkinNode)_atomSpace.AddAtom(skinNode);
    }

    /// <summary>
    /// Creates a link between atoms and adds it to the atomspace.
    /// </summary>
    /// <param name="linkType">The type of link.</param>
    /// <param name="scale">The scale level for the link.</param>
    /// <param name="atoms">The atoms to link.</param>
    /// <returns>The created link.</returns>
    private Link CreateLink(string linkType, ScaleLevel scale, params Atom[] atoms)
    {
        var linkId = $"link_{linkType}_{scale}_{Guid.NewGuid():N}";
        var link = new Link(linkId, linkType, scale, atoms);
        return (Link)_atomSpace.AddAtom(link);
    }

    /// <summary>
    /// Gets default truth values for different component types.
    /// </summary>
    /// <param name="componentType">The component type.</param>
    /// <returns>Default truth value for the component.</returns>
    private TruthValue GetDefaultTruthValueForComponent(SkinComponentType componentType)
    {
        return componentType switch
        {
            // High confidence for well-established molecular components
            SkinComponentType.Protein => new TruthValue(0.95, 0.99),
            SkinComponentType.Lipid => new TruthValue(0.90, 0.95),
            
            // High confidence for established cell types
            SkinComponentType.Keratinocyte => new TruthValue(0.98, 0.99),
            SkinComponentType.Melanocyte => new TruthValue(0.95, 0.98),
            SkinComponentType.Fibroblast => new TruthValue(0.96, 0.98),
            SkinComponentType.LangerhansCell => new TruthValue(0.90, 0.95),
            
            // Very high confidence for tissue structures
            SkinComponentType.Epidermis => new TruthValue(0.99, 0.99),
            SkinComponentType.Dermis => new TruthValue(0.99, 0.99),
            SkinComponentType.Hypodermis => new TruthValue(0.98, 0.99),
            SkinComponentType.HairFollicle => new TruthValue(0.95, 0.98),
            
            // Maximum confidence for organ level
            SkinComponentType.SkinSystem => new TruthValue(1.0, 1.0),
            
            _ => new TruthValue(0.8, 0.9)
        };
    }
}

/// <summary>
/// Customization options for the multiscale skin model.
/// </summary>
public class SkinModelCustomization
{
    /// <summary>
    /// Enabled scale levels for the model.
    /// </summary>
    public HashSet<ScaleLevel> EnabledScales { get; set; } = new() 
    { 
        ScaleLevel.Molecular, 
        ScaleLevel.Cellular, 
        ScaleLevel.Tissue, 
        ScaleLevel.Organ 
    };

    /// <summary>
    /// Custom components to add at each scale level.
    /// </summary>
    public Dictionary<ScaleLevel, List<(string name, SkinComponentType type, object? data)>> CustomComponents { get; set; } = new();

    /// <summary>
    /// Custom relationship types to create between components.
    /// </summary>
    public List<(string linkType, string sourceComponent, string targetComponent, ScaleLevel scale)> CustomRelationships { get; set; } = new();

    /// <summary>
    /// Custom truth values for specific components.
    /// </summary>
    public Dictionary<string, TruthValue> CustomTruthValues { get; set; } = new();
}

/// <summary>
/// Factory for creating customized multiscale skin models.
/// </summary>
public static class SkinModelFactory
{
    /// <summary>
    /// Creates a customized skin model based on the provided customization options.
    /// </summary>
    /// <param name="atomSpace">The atomspace to create the model in.</param>
    /// <param name="customization">Customization options for the model.</param>
    /// <returns>A collection of created atoms representing the customized skin model.</returns>
    public static ICollection<Atom> CreateCustomizedSkinModel(AtomSpace atomSpace, SkinModelCustomization customization)
    {
        var builder = new MultiscaleSkinModelBuilder(atomSpace);
        var createdAtoms = new List<Atom>();

        // Start with basic model if all scales are enabled
        if (customization.EnabledScales.Count == 4 && 
            customization.EnabledScales.SetEquals(new[] { ScaleLevel.Molecular, ScaleLevel.Cellular, ScaleLevel.Tissue, ScaleLevel.Organ }))
        {
            createdAtoms.AddRange(builder.CreateBasicSkinModel());
        }

        // Add custom components
        foreach (var (scale, components) in customization.CustomComponents)
        {
            if (!customization.EnabledScales.Contains(scale))
                continue;

            foreach (var (name, type, data) in components)
            {
                var atomId = $"custom_{scale}_{name}_{Guid.NewGuid():N}";
                var truthValue = customization.CustomTruthValues.TryGetValue(name, out var tv) 
                    ? tv 
                    : new TruthValue(0.8, 0.9);

                var skinNode = new SkinNode(atomId, name, scale, type, data, truthValue);
                createdAtoms.Add(atomSpace.AddAtom(skinNode));
            }
        }

        // Add custom relationships
        var atomsDict = createdAtoms.OfType<SkinNode>().ToDictionary(n => n.Name, n => n);
        foreach (var (linkType, sourceComponent, targetComponent, scale) in customization.CustomRelationships)
        {
            if (!customization.EnabledScales.Contains(scale))
                continue;

            if (atomsDict.TryGetValue(sourceComponent, out var source) && 
                atomsDict.TryGetValue(targetComponent, out var target))
            {
                var linkId = $"custom_link_{linkType}_{scale}_{Guid.NewGuid():N}";
                var link = new Link(linkId, linkType, scale, new[] { source, target });
                createdAtoms.Add(atomSpace.AddAtom(link));
            }
        }

        return createdAtoms;
    }
}