// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.VisualStudio.LanguageServices.Options;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings;

public sealed class NamingSettingsConfigurationTests
{
    [Fact]
    public void ReadableIdentityEditIsNotTheSameConfiguration()
    {
        var graph = NamingSettingsGraph.FromPreferences(NamingStylePreferences.Default);
        var first = graph.SymbolSpecifications[0].ToDictionary(pair => pair.Key, pair => pair.Value);
        first["id"] = "renamed_class_id";
        var edited = new NamingSettingsGraph(graph.SymbolSpecifications.SetItem(0, first), graph.NamingStyles, graph.Rules);

        Assert.NotEqual(graph.ToPreferences(), edited.ToPreferences());
        Assert.False(graph.ConfigurationEquals(edited));
    }

    [Fact]
    public void CapturedDefaultNamesAndLocalizedMarkersHaveTheSameCurrentConfiguration()
    {
        var graph = NamingSettingsGraph.Default;
        var explicitNames = NamingSettingsGraph.FromPreferences(graph.ToPreferences());

        Assert.Equal(graph.ToPreferences(), explicitNames.ToPreferences());
        Assert.True(graph.ConfigurationEquals(explicitNames));
    }

    [Fact]
    public void MissingAndFalseDefaultNameMetadataHaveTheSameMeaning()
    {
        var graph = NamingSettingsGraph.FromPreferences(NamingStylePreferences.Default);
        var first = graph.SymbolSpecifications[0].ToDictionary(pair => pair.Key, pair => pair.Value);
        first["defaultName"] = false;
        var expanded = new NamingSettingsGraph(graph.SymbolSpecifications.SetItem(0, first), graph.NamingStyles, graph.Rules);

        Assert.True(graph.ConfigurationEquals(expanded));
    }

    [Fact]
    public void ReconstructedCollectionsCompareByConfigurationNotBackingArray()
    {
        var graph = NamingSettingsGraph.FromPreferences(NamingStylePreferences.Default);
        var specifications = graph.SymbolSpecifications.Select(item => (IReadOnlyDictionary<string, object>)item.ToDictionary(pair => pair.Key, pair => pair.Value)).ToImmutableArray();
        var reconstructed = new NamingSettingsGraph(specifications, graph.NamingStyles, graph.Rules);

        Assert.True(graph.ConfigurationEquals(reconstructed));
    }
}
