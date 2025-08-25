// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.SemanticKernel.Plugins.AtomSpace;

/// <summary>
/// Represents a node atom that contains data or concepts.
/// </summary>
public class Node : Atom
{
    /// <summary>
    /// The name or label of this node.
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// The data payload of this node.
    /// </summary>
    public object? Data { get; set; }

    public Node(string id, string name, ScaleLevel scale, object? data = null, TruthValue? truthValue = null)
        : base(id, AtomType.Node, scale, truthValue)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Data = data;
    }

    public override string ToString()
    {
        return $"Node({Name}, Scale: {Scale})";
    }
}

/// <summary>
/// Represents a link atom that connects other atoms.
/// </summary>
public class Link : Atom
{
    /// <summary>
    /// The name or type of this link.
    /// </summary>
    public string LinkType { get; }
    
    /// <summary>
    /// The atoms that this link connects.
    /// </summary>
    public IReadOnlyList<Atom> OutgoingSet { get; }

    public Link(string id, string linkType, ScaleLevel scale, IEnumerable<Atom> outgoingSet, TruthValue? truthValue = null)
        : base(id, AtomType.Link, scale, truthValue)
    {
        LinkType = linkType ?? throw new ArgumentNullException(nameof(linkType));
        OutgoingSet = outgoingSet?.ToList().AsReadOnly() ?? throw new ArgumentNullException(nameof(outgoingSet));
        
        if (OutgoingSet.Count == 0)
            throw new ArgumentException("Link must connect at least one atom", nameof(outgoingSet));
    }

    public override string ToString()
    {
        return $"Link({LinkType}, Scale: {Scale}, Arity: {OutgoingSet.Count})";
    }
}

/// <summary>
/// Specialized node for representing skin-related concepts.
/// </summary>
public class SkinNode : Node
{
    /// <summary>
    /// The biological component type this node represents.
    /// </summary>
    public SkinComponentType ComponentType { get; }

    public SkinNode(string id, string name, ScaleLevel scale, SkinComponentType componentType, object? data = null, TruthValue? truthValue = null)
        : base(id, name, scale, data, truthValue)
    {
        ComponentType = componentType;
    }

    public override string ToString()
    {
        return $"SkinNode({Name}, Scale: {Scale}, Type: {ComponentType})";
    }
}

/// <summary>
/// Types of skin components at different scales.
/// </summary>
public enum SkinComponentType
{
    // Molecular level
    Protein,
    Lipid,
    DNA,
    RNA,
    Enzyme,
    
    // Cellular level
    Keratinocyte,
    Melanocyte,
    LangerhansCell,
    Fibroblast,
    
    // Tissue level
    Epidermis,
    Dermis,
    Hypodermis,
    HairFollicle,
    
    // Organ level
    SkinRegion,
    SkinSystem
}