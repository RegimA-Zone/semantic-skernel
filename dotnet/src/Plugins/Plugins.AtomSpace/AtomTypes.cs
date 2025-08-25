// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;

namespace Microsoft.SemanticKernel.Plugins.AtomSpace;

/// <summary>
/// Represents the type of an atom in the atomspace.
/// </summary>
public enum AtomType
{
    /// <summary>
    /// A node atom that contains data or concepts.
    /// </summary>
    Node,
    
    /// <summary>
    /// A link atom that connects other atoms.
    /// </summary>
    Link
}

/// <summary>
/// Represents the scale level in a multiscale model.
/// </summary>
public enum ScaleLevel
{
    /// <summary>
    /// Molecular level - individual molecules and their interactions.
    /// </summary>
    Molecular = 0,
    
    /// <summary>
    /// Cellular level - cell structures and cell-level processes.
    /// </summary>
    Cellular = 1,
    
    /// <summary>
    /// Tissue level - tissue organization and properties.
    /// </summary>
    Tissue = 2,
    
    /// <summary>
    /// Organ level - organ-level structure and function.
    /// </summary>
    Organ = 3
}

/// <summary>
/// Base class for all atoms in the atomspace.
/// </summary>
public abstract class Atom : IEquatable<Atom>
{
    /// <summary>
    /// Unique identifier for the atom.
    /// </summary>
    public string Id { get; }
    
    /// <summary>
    /// The type of this atom.
    /// </summary>
    public AtomType Type { get; }
    
    /// <summary>
    /// The scale level this atom represents.
    /// </summary>
    public ScaleLevel Scale { get; }
    
    /// <summary>
    /// Truth value representing the confidence in this atom.
    /// </summary>
    public TruthValue TruthValue { get; set; }
    
    /// <summary>
    /// Metadata associated with this atom.
    /// </summary>
    public Dictionary<string, object> Metadata { get; }

    protected Atom(string id, AtomType type, ScaleLevel scale, TruthValue? truthValue = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Type = type;
        Scale = scale;
        TruthValue = truthValue ?? new TruthValue(1.0, 1.0);
        Metadata = new Dictionary<string, object>();
    }

    public bool Equals(Atom? other)
    {
        return other is not null && Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Atom);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}

/// <summary>
/// Represents a truth value with strength and confidence.
/// </summary>
public class TruthValue
{
    /// <summary>
    /// The strength of the truth value (0.0 to 1.0).
    /// </summary>
    public double Strength { get; set; }
    
    /// <summary>
    /// The confidence in the truth value (0.0 to 1.0).
    /// </summary>
    public double Confidence { get; set; }

    public TruthValue(double strength, double confidence)
    {
        if (strength < 0.0 || strength > 1.0)
            throw new ArgumentOutOfRangeException(nameof(strength), "Strength must be between 0.0 and 1.0");
        if (confidence < 0.0 || confidence > 1.0)
            throw new ArgumentOutOfRangeException(nameof(confidence), "Confidence must be between 0.0 and 1.0");
            
        Strength = strength;
        Confidence = confidence;
    }
}