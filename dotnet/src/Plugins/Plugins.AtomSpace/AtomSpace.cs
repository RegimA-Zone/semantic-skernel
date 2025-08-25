// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SemanticKernel.Plugins.AtomSpace;

/// <summary>
/// The main AtomSpace class that manages atoms and their relationships.
/// </summary>
public class AtomSpace
{
    private readonly ConcurrentDictionary<string, Atom> _atoms = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _incoming = new();
    private readonly object _lock = new();

    /// <summary>
    /// Configuration for the atomspace.
    /// </summary>
    public AtomSpaceConfiguration Configuration { get; }

    /// <summary>
    /// Gets all atoms in the atomspace.
    /// </summary>
    public IEnumerable<Atom> Atoms => _atoms.Values;

    /// <summary>
    /// Gets the count of atoms in the atomspace.
    /// </summary>
    public int Count => _atoms.Count;

    public AtomSpace(AtomSpaceConfiguration? configuration = null)
    {
        Configuration = configuration ?? new AtomSpaceConfiguration();
    }

    /// <summary>
    /// Adds an atom to the atomspace.
    /// </summary>
    /// <param name="atom">The atom to add.</param>
    /// <returns>The added atom, or existing atom if one with the same ID exists.</returns>
    public Atom AddAtom(Atom atom)
    {
        if (atom == null)
            throw new ArgumentNullException(nameof(atom));

        lock (_lock)
        {
            if (_atoms.TryGetValue(atom.Id, out var existing))
            {
                // Update truth value if the new one has higher confidence
                if (atom.TruthValue.Confidence > existing.TruthValue.Confidence)
                {
                    existing.TruthValue = atom.TruthValue;
                }
                return existing;
            }

            _atoms[atom.Id] = atom;
            _incoming[atom.Id] = new HashSet<string>();

            // If this is a link, update incoming sets
            if (atom is Link link)
            {
                foreach (var target in link.OutgoingSet)
                {
                    _incoming.GetOrAdd(target.Id, _ => new HashSet<string>()).Add(atom.Id);
                }
            }

            return atom;
        }
    }

    /// <summary>
    /// Gets an atom by its ID.
    /// </summary>
    /// <param name="id">The atom ID.</param>
    /// <returns>The atom if found, null otherwise.</returns>
    public Atom? GetAtom(string id)
    {
        return _atoms.TryGetValue(id, out var atom) ? atom : null;
    }

    /// <summary>
    /// Removes an atom from the atomspace.
    /// </summary>
    /// <param name="id">The ID of the atom to remove.</param>
    /// <returns>True if the atom was removed, false if it didn't exist.</returns>
    public bool RemoveAtom(string id)
    {
        lock (_lock)
        {
            if (!_atoms.TryRemove(id, out var atom))
                return false;

            // Remove from incoming sets
            if (atom is Link link)
            {
                foreach (var target in link.OutgoingSet)
                {
                    if (_incoming.TryGetValue(target.Id, out var incomingSet))
                    {
                        incomingSet.Remove(id);
                    }
                }
            }

            // Remove incoming references to this atom
            if (_incoming.TryGetValue(id, out var incoming))
            {
                foreach (var linkId in incoming.ToList())
                {
                    RemoveAtom(linkId);
                }
                _incoming.TryRemove(id, out _);
            }

            return true;
        }
    }

    /// <summary>
    /// Gets all atoms of a specific type.
    /// </summary>
    /// <param name="atomType">The type of atoms to retrieve.</param>
    /// <returns>Atoms of the specified type.</returns>
    public IEnumerable<Atom> GetAtomsByType(AtomType atomType)
    {
        return _atoms.Values.Where(a => a.Type == atomType);
    }

    /// <summary>
    /// Gets all atoms at a specific scale level.
    /// </summary>
    /// <param name="scale">The scale level.</param>
    /// <returns>Atoms at the specified scale.</returns>
    public IEnumerable<Atom> GetAtomsByScale(ScaleLevel scale)
    {
        return _atoms.Values.Where(a => a.Scale == scale);
    }

    /// <summary>
    /// Gets incoming links for an atom.
    /// </summary>
    /// <param name="atomId">The target atom ID.</param>
    /// <returns>Links that point to the specified atom.</returns>
    public IEnumerable<Link> GetIncomingLinks(string atomId)
    {
        if (!_incoming.TryGetValue(atomId, out var incomingIds))
            yield break;

        foreach (var linkId in incomingIds)
        {
            if (_atoms.TryGetValue(linkId, out var atom) && atom is Link link)
                yield return link;
        }
    }

    /// <summary>
    /// Clears all atoms from the atomspace.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _atoms.Clear();
            _incoming.Clear();
        }
    }

    /// <summary>
    /// Creates a query builder for complex queries.
    /// </summary>
    /// <returns>A new query builder instance.</returns>
    public AtomSpaceQuery Query()
    {
        return new AtomSpaceQuery(this);
    }
}

/// <summary>
/// Configuration options for the AtomSpace.
/// </summary>
public class AtomSpaceConfiguration
{
    /// <summary>
    /// Maximum number of atoms allowed in the atomspace.
    /// </summary>
    public int MaxAtoms { get; set; } = 100000;

    /// <summary>
    /// Whether to enable automatic garbage collection of low-confidence atoms.
    /// </summary>
    public bool EnableAutomaticGarbageCollection { get; set; } = false;

    /// <summary>
    /// Minimum confidence threshold for atom retention during garbage collection.
    /// </summary>
    public double MinimumConfidenceThreshold { get; set; } = 0.1;

    /// <summary>
    /// Configuration specific to multiscale skin modeling.
    /// </summary>
    public MultiscaleSkinConfiguration SkinConfiguration { get; set; } = new();
}

/// <summary>
/// Configuration specific to multiscale skin modeling.
/// </summary>
public class MultiscaleSkinConfiguration
{
    /// <summary>
    /// Enabled scale levels for skin modeling.
    /// </summary>
    public HashSet<ScaleLevel> EnabledScales { get; set; } = new() { ScaleLevel.Molecular, ScaleLevel.Cellular, ScaleLevel.Tissue, ScaleLevel.Organ };

    /// <summary>
    /// Scale transition rules for connecting atoms between scales.
    /// </summary>
    public Dictionary<(ScaleLevel from, ScaleLevel to), double> ScaleTransitionWeights { get; set; } = new()
    {
        { (ScaleLevel.Molecular, ScaleLevel.Cellular), 0.8 },
        { (ScaleLevel.Cellular, ScaleLevel.Tissue), 0.8 },
        { (ScaleLevel.Tissue, ScaleLevel.Organ), 0.8 }
    };

    /// <summary>
    /// Default truth values for different skin component types.
    /// </summary>
    public Dictionary<SkinComponentType, TruthValue> DefaultTruthValues { get; set; } = new();
}