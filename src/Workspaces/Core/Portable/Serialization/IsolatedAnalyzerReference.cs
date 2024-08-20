// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET

using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Runtime.Loader;

namespace Microsoft.CodeAnalysis.Serialization;

/// <summary>
/// Wrapper around a real <see cref="AnalyzerReference"/>.  An "isolated" analyzer reference is an analyzer reference
/// associated with an <see cref="AssemblyLoadContext"/> that is connected to a set of other "isolated" analyzer
/// references.  This allows for loading the analyzers and generators from it in a way that is associated with that load
/// context, keeping them separate from other analyzers and generators loaded in other load contexts, while also
/// allowing all of those instances to be collected when no longer needed.  Being isolated means that if any of the
/// underlying assembly references change, that they can be loaded side by side with the prior references.  This enables
/// functionality like live reloading of analyzers and generators when they change on disk.  Note: this is only
/// supported on .Net Core, and not .Net Framework, as only the former has <see cref="AssemblyLoadContext"/>s.
/// </summary>
/// <remarks>
/// The purpose of this type is to allow passing out a <see cref="AnalyzerReference"/> to the rest of the system that
/// then ensures that as long as it is alive (or any <see cref="DiagnosticAnalyzer"/> or <see cref="ISourceGenerator"/>
/// it passes out is alive), that the <see cref="IsolatedAssemblyReferenceSet"/> (and its corresponding <see
/// cref="AssemblyLoadContext"/>) is kept alive as well.
/// </remarks>
internal sealed class IsolatedAnalyzerReference(
    IsolatedAssemblyReferenceSet isolatedAssemblyReferenceSet,
    AnalyzerReference underlyingAnalyzerReference) : AnalyzerReference
{
    /// <summary>
    /// Conditional weak tables that ensure that as long as a particular <see cref="DiagnosticAnalyzer"/> or <see
    /// cref="ISourceGenerator"/> is alive, that the corresponding <see cref="IsolatedAssemblyReferenceSet"/> (and its
    /// corresponding <see cref="AssemblyLoadContext"/> is kept alive.
    /// </summary>
    private static readonly ConditionalWeakTable<DiagnosticAnalyzer, IsolatedAssemblyReferenceSet> s_analyzerToPinnedReferenceSet = [];

    /// <inheritdoc cref="s_analyzerToPinnedReferenceSet"/>
    private static readonly ConditionalWeakTable<ISourceGenerator, IsolatedAssemblyReferenceSet> s_generatorToPinnedReferenceSet = [];

    /// <summary>
    /// We keep a strong reference here.  As long as this <see cref="IsolatedAnalyzerReference"/> is passed out and held
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
        => $"{nameof(IsolatedAnalyzerReference)}({UnderlyingAnalyzerReference})";
}

#endif
