// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Collections;

namespace Microsoft.CodeAnalysis;

using AnalyzerReferencesToSourceGenerators = ConditionalWeakTable<IReadOnlyList<AnalyzerReference>, SolutionCompilationState.SourceGeneratorMap>;

internal partial class SolutionCompilationState
{
    internal sealed record SourceGeneratorMap(ImmutableArray<ISourceGenerator> SourceGenerators, ImmutableDictionary<ISourceGenerator, AnalyzerReference> SourceGeneratorToAnalyzerReference)

    /// <summary>
    /// Cached mapping from language (only C#/VB since those are the only languages that support analyzers) to the lists
    /// of analyzer references (see <see cref="ProjectState.AnalyzerReferences"/>) to all the <see
    /// cref="ISourceGenerator"/>s produced by those references.  This should only be created and cached on the OOP side
    /// of things so that we don't cause source generators to be loaded (and fixed) within VS (which is .net framework
    /// only).
    /// </summary>
    private static readonly Dictionary<string, (AnalyzerReferencesToSourceGenerators map, AnalyzerReferencesToSourceGenerators.CreateValueCallback callback)> s_languageToAnalyzerReferencesToSourceGeneratorsMap = new()
    {
        { LanguageNames.CSharp, (new(), (static rs => ComputeSourceGenerators(rs, LanguageNames.CSharp))) },
        { LanguageNames.VisualBasic, (new(), (static rs => ComputeSourceGenerators(rs, LanguageNames.VisualBasic))) },
    };

    private static SourceGeneratorMap ComputeSourceGenerators(IReadOnlyList<AnalyzerReference> analyzerReferences, string language)
    {
        using var generators = TemporaryArray<ISourceGenerator>.Empty;
        var generatorToAnalyzerReference = ImmutableDictionary.CreateBuilder<ISourceGenerator, AnalyzerReference>();

        foreach (var reference in analyzerReferences)
        {
            foreach (var generator in reference.GetGenerators(language).Distinct())
            {
                generators.Add(generator);
                generatorToAnalyzerReference.Add(generator, reference);
            }
        }

        return new(generators.ToImmutableAndClear(), generatorToAnalyzerReference.ToImmutable());
    }

    private static ImmutableArray<ISourceGenerator> GetSourceGenerators(ProjectState projectState)
        => GetSourceGenerators(projectState.Language, projectState.AnalyzerReferences);

    private static ImmutableArray<ISourceGenerator> GetSourceGenerators(string language, IReadOnlyList<AnalyzerReference> analyzerReferences)
    {
        if (!s_languageToAnalyzerReferencesToSourceGeneratorsMap.TryGetValue(language, out var tuple))
            return [];

        var map = tuple.map.GetValue(analyzerReferences, tuple.callback);
        return map.SourceGenerators;
    }
}
