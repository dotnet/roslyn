// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Diagnostics;

#if NET

namespace Microsoft.CodeAnalysis.Serialization;

/// <summary>
/// Wrapper around a real <see cref="AnalyzerReference"/>.
/// </summary>
internal sealed class IsolatedAnalyzerFileReference(
    IsolatedAssemblyReferenceSet isolatedAssemblyReferenceSet,
    AnalyzerReference underlyingAnalyzerReference) : AnalyzerReference
{
    private static readonly ConditionalWeakTable<DiagnosticAnalyzer, IsolatedAssemblyReferenceSet> s_analyzerToPinnedReferenceSet = new();
    private static readonly ConditionalWeakTable<ISourceGenerator, IsolatedAssemblyReferenceSet> s_generatorToPinnedReferenceSet = new();

    /// <summary>
    /// We keep a strong reference here.  As long as the IsolatedAnalyzerFileReference is passed out and held
    /// onto (say by a Project instance), it should keep the IsolatedAssemblyReferenceSet (and its ALC) alive.
    /// </summary>
    private readonly IsolatedAssemblyReferenceSet _isolatedAssemblyReferenceSet = isolatedAssemblyReferenceSet;

    /// <summary>
    /// The actual real <see cref="AnalyzerReference"/> we defer our operations to.
    /// </summary>
    public readonly AnalyzerReference UnderlyingAnalyzerReference = underlyingAnalyzerReference;

    public override string Display => UnderlyingAnalyzerReference.Display;
    public override string? FullPath => UnderlyingAnalyzerReference.FullPath;
    public override object Id => UnderlyingAnalyzerReference.Id;

    public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language)
        => PinAnalyzers(UnderlyingAnalyzerReference.GetAnalyzers(language));

    public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages()
        => PinAnalyzers(UnderlyingAnalyzerReference.GetAnalyzersForAllLanguages());

    [Obsolete]
    public override ImmutableArray<ISourceGenerator> GetGenerators()
        => PinGenerators(UnderlyingAnalyzerReference.GetGenerators());

    public override ImmutableArray<ISourceGenerator> GetGenerators(string language)
        => PinGenerators(UnderlyingAnalyzerReference.GetGenerators(language));

    public override ImmutableArray<ISourceGenerator> GetGeneratorsForAllLanguages()
        => PinGenerators(UnderlyingAnalyzerReference.GetGeneratorsForAllLanguages());

    private ImmutableArray<ISourceGenerator> PinGenerators(ImmutableArray<ISourceGenerator> generators)
    {
        // Keep a reference from each generator to the IsolatedAssemblyReferenceSet.  This will ensure it (and
        // the ALC it points at) stays alive as long as the generator instance stays alive.
        foreach (var generator in generators)
            s_generatorToPinnedReferenceSet.TryAdd(generator, _isolatedAssemblyReferenceSet);

        return generators;
    }

    private ImmutableArray<DiagnosticAnalyzer> PinAnalyzers(ImmutableArray<DiagnosticAnalyzer> analyzers)
    {
        // Keep a reference from each analyzer to the IsolatedAssemblyReferenceSet.  This will ensure it (and
        // the ALC it points at) stays alive as long as the generator instance stays alive.
        foreach (var analyzer in analyzers)
            s_analyzerToPinnedReferenceSet.TryAdd(analyzer, _isolatedAssemblyReferenceSet);

        return analyzers;
    }

    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj);

    public override int GetHashCode()
        => RuntimeHelpers.GetHashCode(this);

    public override string ToString()
        => $"{nameof(IsolatedAnalyzerFileReference)}({UnderlyingAnalyzerReference})";
}

#endif
