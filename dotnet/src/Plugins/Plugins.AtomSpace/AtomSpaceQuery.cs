// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.SemanticKernel.Plugins.AtomSpace;

/// <summary>
/// Query builder for complex AtomSpace queries.
/// </summary>
public class AtomSpaceQuery
{
    private readonly AtomSpace _atomSpace;
    private readonly List<Func<Atom, bool>> _filters = new();

    internal AtomSpaceQuery(AtomSpace atomSpace)
    {
        _atomSpace = atomSpace ?? throw new ArgumentNullException(nameof(atomSpace));
    }

    /// <summary>
    /// Filters atoms by type.
    /// </summary>
    /// <param name="atomType">The atom type to filter by.</param>
    /// <returns>The query builder for chaining.</returns>
    public AtomSpaceQuery OfType(AtomType atomType)
    {
        _filters.Add(atom => atom.Type == atomType);
        return this;
    }

    /// <summary>
    /// Filters atoms by scale level.
    /// </summary>
    /// <param name="scale">The scale level to filter by.</param>
    /// <returns>The query builder for chaining.</returns>
    public AtomSpaceQuery AtScale(ScaleLevel scale)
    {
        _filters.Add(atom => atom.Scale == scale);
        return this;
    }

    /// <summary>
    /// Filters nodes by name pattern.
    /// </summary>
    /// <param name="namePattern">The name pattern to match.</param>
    /// <returns>The query builder for chaining.</returns>
    public AtomSpaceQuery WithNameContaining(string namePattern)
    {
        _filters.Add(atom => atom is Node node && node.Name.Contains(namePattern, StringComparison.OrdinalIgnoreCase));
        return this;
    }

    /// <summary>
    /// Filters links by link type.
    /// </summary>
    /// <param name="linkType">The link type to filter by.</param>
    /// <returns>The query builder for chaining.</returns>
    public AtomSpaceQuery WithLinkType(string linkType)
    {
        _filters.Add(atom => atom is Link link && link.LinkType.Equals(linkType, StringComparison.OrdinalIgnoreCase));
        return this;
    }

    /// <summary>
    /// Filters skin nodes by component type.
    /// </summary>
    /// <param name="componentType">The skin component type to filter by.</param>
    /// <returns>The query builder for chaining.</returns>
    public AtomSpaceQuery WithSkinComponent(SkinComponentType componentType)
    {
        _filters.Add(atom => atom is SkinNode skinNode && skinNode.ComponentType == componentType);
        return this;
    }

    /// <summary>
    /// Filters atoms by minimum truth value strength.
    /// </summary>
    /// <param name="minStrength">The minimum strength threshold.</param>
    /// <returns>The query builder for chaining.</returns>
    public AtomSpaceQuery WithMinStrength(double minStrength)
    {
        _filters.Add(atom => atom.TruthValue.Strength >= minStrength);
        return this;
    }

    /// <summary>
    /// Filters atoms by minimum confidence.
    /// </summary>
    /// <param name="minConfidence">The minimum confidence threshold.</param>
    /// <returns>The query builder for chaining.</returns>
    public AtomSpaceQuery WithMinConfidence(double minConfidence)
    {
        _filters.Add(atom => atom.TruthValue.Confidence >= minConfidence);
        return this;
    }

    /// <summary>
    /// Adds a custom filter.
    /// </summary>
    /// <param name="filter">The custom filter function.</param>
    /// <returns>The query builder for chaining.</returns>
    public AtomSpaceQuery Where(Func<Atom, bool> filter)
    {
        _filters.Add(filter ?? throw new ArgumentNullException(nameof(filter)));
        return this;
    }

    /// <summary>
    /// Executes the query and returns matching atoms.
    /// </summary>
    /// <returns>Atoms that match all filters.</returns>
    public IEnumerable<Atom> Execute()
    {
        var atoms = _atomSpace.Atoms;
        
        foreach (var filter in _filters)
        {
            atoms = atoms.Where(filter);
        }
        
        return atoms.ToList();
    }

    /// <summary>
    /// Executes the query and returns matching atoms as nodes.
    /// </summary>
    /// <returns>Matching atoms cast to nodes.</returns>
    public IEnumerable<Node> ExecuteAsNodes()
    {
        return Execute().OfType<Node>();
    }

    /// <summary>
    /// Executes the query and returns matching atoms as links.
    /// </summary>
    /// <returns>Matching atoms cast to links.</returns>
    public IEnumerable<Link> ExecuteAsLinks()
    {
        return Execute().OfType<Link>();
    }

    /// <summary>
    /// Executes the query and returns matching atoms as skin nodes.
    /// </summary>
    /// <returns>Matching atoms cast to skin nodes.</returns>
    public IEnumerable<SkinNode> ExecuteAsSkinNodes()
    {
        return Execute().OfType<SkinNode>();
    }
}

/// <summary>
/// Extension methods for AtomSpace queries.
/// </summary>
public static class AtomSpaceQueryExtensions
{
    /// <summary>
    /// Finds atoms connected to the specified atom.
    /// </summary>
    /// <param name="atomSpace">The atomspace to search.</param>
    /// <param name="atomId">The atom ID to find connections for.</param>
    /// <param name="maxDepth">Maximum depth to search (default: 1).</param>
    /// <returns>Connected atoms.</returns>
    public static IEnumerable<Atom> FindConnectedAtoms(this AtomSpace atomSpace, string atomId, int maxDepth = 1)
    {
        var visited = new HashSet<string>();
        var toVisit = new Queue<(string id, int depth)>();
        toVisit.Enqueue((atomId, 0));
        visited.Add(atomId);

        while (toVisit.Count > 0)
        {
            var (currentId, depth) = toVisit.Dequeue();
            
            if (depth >= maxDepth)
                continue;

            var currentAtom = atomSpace.GetAtom(currentId);
            if (currentAtom == null)
                continue;

            // Find outgoing connections (if this is a link)
            if (currentAtom is Link link)
            {
                foreach (var target in link.OutgoingSet)
                {
                    if (!visited.Contains(target.Id))
                    {
                        visited.Add(target.Id);
                        toVisit.Enqueue((target.Id, depth + 1));
                        yield return target;
                    }
                }
            }

            // Find incoming connections
            foreach (var incomingLink in atomSpace.GetIncomingLinks(currentId))
            {
                if (!visited.Contains(incomingLink.Id))
                {
                    visited.Add(incomingLink.Id);
                    toVisit.Enqueue((incomingLink.Id, depth + 1));
                    yield return incomingLink;
                }
            }
        }
    }

    /// <summary>
    /// Finds atoms that bridge different scale levels.
    /// </summary>
    /// <param name="atomSpace">The atomspace to search.</param>
    /// <param name="fromScale">Source scale level.</param>
    /// <param name="toScale">Target scale level.</param>
    /// <returns>Bridge atoms connecting the scale levels.</returns>
    public static IEnumerable<Link> FindScaleBridges(this AtomSpace atomSpace, ScaleLevel fromScale, ScaleLevel toScale)
    {
        return atomSpace.Query()
            .OfType(AtomType.Link)
            .ExecuteAsLinks()
            .Where(link => link.OutgoingSet.Any(atom => atom.Scale == fromScale) && 
                          link.OutgoingSet.Any(atom => atom.Scale == toScale));
    }
}