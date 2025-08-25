# AtomSpace Plugin for Multiscale Skin Modeling

This plugin provides atomspace functionality specifically designed for multiscale biological modeling, with a focus on skin system modeling across molecular, cellular, tissue, and organ levels.

## Overview

The AtomSpace plugin enables:
- **Knowledge representation** using atoms (nodes and links) with truth values
- **Multiscale modeling** across four biological scales
- **Customizable skin models** with predefined components and relationships
- **Query capabilities** for complex knowledge discovery
- **Plugin integration** with Semantic Kernel

## Core Components

### Atom Types
- **`Atom`**: Base class for all atoms with truth values and metadata
- **`Node`**: Represents entities/concepts 
- **`Link`**: Represents relationships between atoms
- **`SkinNode`**: Specialized node for skin components

### Scale Levels
- **Molecular**: Proteins, lipids, DNA, RNA, enzymes
- **Cellular**: Keratinocytes, melanocytes, fibroblasts, Langerhans cells
- **Tissue**: Epidermis, dermis, hypodermis, hair follicles
- **Organ**: Skin regions and systems

## Basic Usage

### 1. Create and Configure AtomSpace

```csharp
using Microsoft.SemanticKernel.Plugins.AtomSpace;

// Create atomspace with custom configuration
var config = new AtomSpaceConfiguration
{
    MaxAtoms = 50000,
    EnableAutomaticGarbageCollection = true,
    SkinConfiguration = new MultiscaleSkinConfiguration
    {
        EnabledScales = { ScaleLevel.Molecular, ScaleLevel.Cellular, ScaleLevel.Tissue }
    }
};

var atomSpace = new AtomSpace(config);
```

### 2. Create Basic Skin Model

```csharp
var builder = new MultiscaleSkinModelBuilder(atomSpace);
var skinModel = builder.CreateBasicSkinModel();

Console.WriteLine($"Created {skinModel.Count} atoms representing the skin system");
```

### 3. Add Custom Components

```csharp
// Add a custom protein at molecular level
var customProtein = new SkinNode(
    "custom_protein_1", 
    "CustomCollagen", 
    ScaleLevel.Molecular, 
    SkinComponentType.Protein,
    data: "Type VII collagen variant",
    truthValue: new TruthValue(0.95, 0.99)
);

atomSpace.AddAtom(customProtein);
```

### 4. Create Relationships

```csharp
// Link cellular component to molecular component  
var keratinocyte = atomSpace.Query()
    .WithSkinComponent(SkinComponentType.Keratinocyte)
    .ExecuteAsSkinNodes()
    .First();

var keratin = atomSpace.Query()
    .WithNameContaining("Keratin")
    .ExecuteAsSkinNodes() 
    .First();

var containsLink = new Link(
    "contains_keratin_1",
    "Contains",
    ScaleLevel.Cellular,
    new[] { keratinocyte, keratin },
    new TruthValue(0.9, 0.95)
);

atomSpace.AddAtom(containsLink);
```

### 5. Query the AtomSpace

```csharp
// Find all cellular components
var cellularComponents = atomSpace.Query()
    .AtScale(ScaleLevel.Cellular)
    .WithSkinComponent(SkinComponentType.Keratinocyte)
    .ExecuteAsSkinNodes();

// Find connections across scales
var bridges = atomSpace.FindScaleBridges(ScaleLevel.Molecular, ScaleLevel.Cellular);

// Complex query with multiple filters
var results = atomSpace.Query()
    .OfType(AtomType.Node)
    .AtScale(ScaleLevel.Tissue)
    .WithMinConfidence(0.8)
    .Where(atom => atom.Metadata.ContainsKey("location"))
    .Execute();
```

## Using as Semantic Kernel Plugin

### 1. Setup Plugin

```csharp
using Microsoft.SemanticKernel;

var kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(/* configuration */)
    .Build();

// Create atomspace and plugin
var atomSpace = new AtomSpace();
var atomSpacePlugin = new AtomSpacePlugin(atomSpace);

// Add to kernel
kernel.ImportPluginFromObject(atomSpacePlugin, "AtomSpace");
```

### 2. Use Plugin Functions

```csharp
// Add skin components via kernel function
var result = await kernel.InvokeAsync("AtomSpace", "AddSkinNode", new()
{
    ["atomName"] = "Elastin",
    ["scaleLevel"] = "Molecular", 
    ["componentType"] = "Protein",
    ["data"] = "Provides skin elasticity"
});

// Query atomspace
var queryResult = await kernel.InvokeAsync("AtomSpace", "QueryAtoms", new()
{
    ["scaleLevel"] = "Cellular",
    ["componentType"] = "Keratinocyte"
});

// Get statistics
var stats = await kernel.InvokeAsync("AtomSpace", "GetAtomSpaceStatistics");
```

## Advanced Customization

### Custom Skin Model

```csharp
var customization = new SkinModelCustomization
{
    EnabledScales = { ScaleLevel.Molecular, ScaleLevel.Cellular },
    CustomComponents = 
    {
        [ScaleLevel.Molecular] = new[]
        {
            ("Hyaluronic Acid", SkinComponentType.Lipid, "Moisture retention"),
            ("Vitamin C", SkinComponentType.Enzyme, "Antioxidant protection")
        }
    },
    CustomRelationships = 
    {
        ("Synthesizes", "Fibroblast", "Hyaluronic Acid", ScaleLevel.Cellular)
    },
    CustomTruthValues = 
    {
        ["Hyaluronic Acid"] = new TruthValue(0.92, 0.98)
    }
};

var customModel = SkinModelFactory.CreateCustomizedSkinModel(atomSpace, customization);
```

### Working with Truth Values

```csharp
// Truth values represent strength (relevance) and confidence
var truthValue = new TruthValue(
    strength: 0.85,    // 85% strength/relevance
    confidence: 0.92   // 92% confidence in this assessment
);

// Update atom truth values based on new evidence
var atom = atomSpace.GetAtom("some_atom_id");
if (atom != null && newEvidence.Confidence > atom.TruthValue.Confidence)
{
    atom.TruthValue = newEvidence;
}
```

## Architecture Notes

- **Thread-safe**: AtomSpace operations are thread-safe for concurrent access
- **Memory efficient**: Uses concurrent dictionaries and lazy evaluation
- **Extensible**: Easy to add new component types and scale levels  
- **Integration ready**: Works seamlessly with Semantic Kernel's plugin system

## Example Applications

1. **Skin Disease Modeling**: Model pathological changes across scales
2. **Drug Discovery**: Track molecular interactions and their tissue-level effects
3. **Cosmetic Research**: Understand ingredient effects from molecular to visible outcomes
4. **Educational Tools**: Interactive exploration of skin biology
5. **Research Analysis**: Knowledge discovery in dermatological research

This plugin provides a foundation for sophisticated biological modeling while maintaining the flexibility to adapt to various research and application needs.