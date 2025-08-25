// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.SemanticKernel.Plugins.AtomSpace;

/// <summary>
/// AtomSpacePlugin provides functionality for managing and querying atomspace structures
/// with support for multiscale skin modeling.
/// </summary>
[Experimental("SKEXP0001")]
public sealed class AtomSpacePlugin
{
    /// <summary>
    /// Name used to specify the atom ID parameter.
    /// </summary>
    public const string AtomIdParam = "atomId";
    
    /// <summary>
    /// Name used to specify the atom name parameter.
    /// </summary>
    public const string AtomNameParam = "atomName";
    
    /// <summary>
    /// Name used to specify the scale level parameter.
    /// </summary>
    public const string ScaleLevelParam = "scaleLevel";
    
    /// <summary>
    /// Name used to specify the component type parameter.
    /// </summary>
    public const string ComponentTypeParam = "componentType";
    
    /// <summary>
    /// Name used to specify the link type parameter.
    /// </summary>
    public const string LinkTypeParam = "linkType";
    
    /// <summary>
    /// Name used to specify the query parameter.
    /// </summary>
    public const string QueryParam = "query";

    private readonly AtomSpace _atomSpace;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the AtomSpacePlugin class.
    /// </summary>
    /// <param name="atomSpace">The atomspace instance to use.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public AtomSpacePlugin(AtomSpace atomSpace, ILogger<AtomSpacePlugin>? logger = null)
    {
        _atomSpace = atomSpace ?? throw new ArgumentNullException(nameof(atomSpace));
        _logger = logger ?? NullLogger<AtomSpacePlugin>.Instance;
    }

    /// <summary>
    /// Adds a skin node to the atomspace.
    /// </summary>
    /// <param name="atomName">The name of the skin component.</param>
    /// <param name="scaleLevel">The scale level (Molecular, Cellular, Tissue, Organ).</param>
    /// <param name="componentType">The type of skin component.</param>
    /// <param name="data">Optional data payload for the node.</param>
    /// <returns>Information about the created node.</returns>
    [KernelFunction, Description("Adds a skin node to the atomspace at the specified scale level")]
    public string AddSkinNode(
        [Description("The name of the skin component")] string atomName,
        [Description("The scale level: Molecular, Cellular, Tissue, or Organ")] string scaleLevel,
        [Description("The type of skin component")] string componentType,
        [Description("Optional data payload for the node")] string? data = null)
    {
        try
        {
            if (!Enum.TryParse<ScaleLevel>(scaleLevel, true, out var scale))
            {
                return $"Invalid scale level: {scaleLevel}. Valid values are: Molecular, Cellular, Tissue, Organ";
            }

            if (!Enum.TryParse<SkinComponentType>(componentType, true, out var skinComponentType))
            {
                return $"Invalid component type: {componentType}";
            }

            var atomId = $"skin_{scale}_{atomName}_{Guid.NewGuid():N}";
            var truthValue = GetDefaultTruthValue(skinComponentType);
            
            var skinNode = new SkinNode(atomId, atomName, scale, skinComponentType, data, truthValue);
            var addedAtom = _atomSpace.AddAtom(skinNode);

            _logger.LogInformation("Added skin node {AtomName} at scale {Scale} with type {ComponentType}", 
                atomName, scale, skinComponentType);

            return JsonSerializer.Serialize(new
            {
                Id = addedAtom.Id,
                Name = atomName,
                Scale = scale.ToString(),
                ComponentType = skinComponentType.ToString(),
                TruthValue = new { addedAtom.TruthValue.Strength, addedAtom.TruthValue.Confidence }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding skin node {AtomName}", atomName);
            return $"Error adding skin node: {ex.Message}";
        }
    }

    /// <summary>
    /// Creates a link between atoms in the atomspace.
    /// </summary>
    /// <param name="linkType">The type of link to create.</param>
    /// <param name="atomIds">Comma-separated list of atom IDs to link.</param>
    /// <param name="scaleLevel">The scale level for the link.</param>
    /// <returns>Information about the created link.</returns>
    [KernelFunction, Description("Creates a link between atoms in the atomspace")]
    public string CreateLink(
        [Description("The type of link to create")] string linkType,
        [Description("Comma-separated list of atom IDs to link")] string atomIds,
        [Description("The scale level for the link")] string scaleLevel)
    {
        try
        {
            if (!Enum.TryParse<ScaleLevel>(scaleLevel, true, out var scale))
            {
                return $"Invalid scale level: {scaleLevel}";
            }

            var ids = atomIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                             .Select(id => id.Trim())
                             .ToList();

            if (ids.Count < 2)
            {
                return "At least two atom IDs are required to create a link";
            }

            var atoms = new List<Atom>();
            foreach (var id in ids)
            {
                var atom = _atomSpace.GetAtom(id);
                if (atom == null)
                {
                    return $"Atom with ID {id} not found";
                }
                atoms.Add(atom);
            }

            var linkId = $"link_{linkType}_{scale}_{Guid.NewGuid():N}";
            var link = new Link(linkId, linkType, scale, atoms);
            var addedLink = _atomSpace.AddAtom(link);

            _logger.LogInformation("Created link {LinkType} connecting {AtomCount} atoms at scale {Scale}", 
                linkType, atoms.Count, scale);

            return JsonSerializer.Serialize(new
            {
                Id = addedLink.Id,
                LinkType = linkType,
                Scale = scale.ToString(),
                ConnectedAtoms = atoms.Select(a => new { a.Id, Type = a.GetType().Name }).ToArray()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating link {LinkType}", linkType);
            return $"Error creating link: {ex.Message}";
        }
    }

    /// <summary>
    /// Queries atoms in the atomspace based on various criteria.
    /// </summary>
    /// <param name="scaleLevel">Optional scale level filter.</param>
    /// <param name="componentType">Optional skin component type filter.</param>
    /// <param name="atomName">Optional name filter (partial match).</param>
    /// <returns>JSON representation of matching atoms.</returns>
    [KernelFunction, Description("Queries atoms in the atomspace based on various criteria")]
    public string QueryAtoms(
        [Description("Optional scale level filter")] string? scaleLevel = null,
        [Description("Optional skin component type filter")] string? componentType = null,
        [Description("Optional name filter (partial match)")] string? atomName = null)
    {
        try
        {
            var query = _atomSpace.Query();

            if (!string.IsNullOrEmpty(scaleLevel) && Enum.TryParse<ScaleLevel>(scaleLevel, true, out var scale))
            {
                query = query.AtScale(scale);
            }

            if (!string.IsNullOrEmpty(componentType) && Enum.TryParse<SkinComponentType>(componentType, true, out var skinComponentType))
            {
                query = query.WithSkinComponent(skinComponentType);
            }

            if (!string.IsNullOrEmpty(atomName))
            {
                query = query.WithNameContaining(atomName);
            }

            var results = query.Execute().Take(50).Select(atom => new
            {
                Id = atom.Id,
                Type = atom.GetType().Name,
                Scale = atom.Scale.ToString(),
                Name = atom is Node node ? node.Name : null,
                LinkType = atom is Link link ? link.LinkType : null,
                ComponentType = atom is SkinNode skinNode ? skinNode.ComponentType.ToString() : null,
                TruthValue = new { atom.TruthValue.Strength, atom.TruthValue.Confidence }
            }).ToArray();

            _logger.LogInformation("Query returned {ResultCount} atoms", results.Length);

            return JsonSerializer.Serialize(new { Count = results.Length, Atoms = results });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying atoms");
            return $"Error querying atoms: {ex.Message}";
        }
    }

    /// <summary>
    /// Finds atoms connected to a specific atom across different scales.
    /// </summary>
    /// <param name="atomId">The ID of the atom to find connections for.</param>
    /// <param name="maxDepth">Maximum connection depth to search.</param>
    /// <returns>JSON representation of connected atoms.</returns>
    [KernelFunction, Description("Finds atoms connected to a specific atom across different scales")]
    public string FindConnectedAtoms(
        [Description("The ID of the atom to find connections for")] string atomId,
        [Description("Maximum connection depth to search (default: 2)")] int maxDepth = 2)
    {
        try
        {
            var sourceAtom = _atomSpace.GetAtom(atomId);
            if (sourceAtom == null)
            {
                return $"Atom with ID {atomId} not found";
            }

            var connected = _atomSpace.FindConnectedAtoms(atomId, maxDepth)
                                     .Take(50)
                                     .Select(atom => new
                                     {
                                         Id = atom.Id,
                                         Type = atom.GetType().Name,
                                         Scale = atom.Scale.ToString(),
                                         Name = atom is Node node ? node.Name : null,
                                         LinkType = atom is Link link ? link.LinkType : null,
                                         ComponentType = atom is SkinNode skinNode ? skinNode.ComponentType.ToString() : null
                                     }).ToArray();

            _logger.LogInformation("Found {ConnectedCount} connected atoms for {AtomId}", connected.Length, atomId);

            return JsonSerializer.Serialize(new 
            { 
                SourceAtom = new { sourceAtom.Id, Type = sourceAtom.GetType().Name, Scale = sourceAtom.Scale.ToString() },
                ConnectedAtoms = connected,
                MaxDepth = maxDepth
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding connected atoms for {AtomId}", atomId);
            return $"Error finding connected atoms: {ex.Message}";
        }
    }

    /// <summary>
    /// Gets statistics about the atomspace.
    /// </summary>
    /// <returns>JSON representation of atomspace statistics.</returns>
    [KernelFunction, Description("Gets statistics about the current state of the atomspace")]
    public string GetAtomSpaceStatistics()
    {
        try
        {
            var atoms = _atomSpace.Atoms.ToList();
            var nodeCount = atoms.Count(a => a.Type == AtomType.Node);
            var linkCount = atoms.Count(a => a.Type == AtomType.Link);
            
            var scaleDistribution = atoms.GroupBy(a => a.Scale)
                                        .ToDictionary(g => g.Key.ToString(), g => g.Count());
            
            var skinNodes = atoms.OfType<SkinNode>().ToList();
            var componentDistribution = skinNodes.GroupBy(sn => sn.ComponentType)
                                                .ToDictionary(g => g.Key.ToString(), g => g.Count());

            var stats = new
            {
                TotalAtoms = _atomSpace.Count,
                Nodes = nodeCount,
                Links = linkCount,
                SkinNodes = skinNodes.Count,
                ScaleDistribution = scaleDistribution,
                ComponentDistribution = componentDistribution,
                Configuration = new
                {
                    MaxAtoms = _atomSpace.Configuration.MaxAtoms,
                    EnabledScales = _atomSpace.Configuration.SkinConfiguration.EnabledScales.Select(s => s.ToString()).ToArray()
                }
            };

            _logger.LogInformation("Generated atomspace statistics: {TotalAtoms} total atoms", stats.TotalAtoms);

            return JsonSerializer.Serialize(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating atomspace statistics");
            return $"Error generating statistics: {ex.Message}";
        }
    }

    private TruthValue GetDefaultTruthValue(SkinComponentType componentType)
    {
        return _atomSpace.Configuration.SkinConfiguration.DefaultTruthValues.TryGetValue(componentType, out var tv) 
            ? tv 
            : new TruthValue(0.8, 0.9); // Default high confidence values
    }
}