// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET

using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Runtime.Loader;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

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
/// it passes out is alive), that the <see cref="IsolatedAnalyzerReferenceSet"/> (and its corresponding <see
/// cref="AssemblyLoadContext"/>) is kept alive as well.
/// </remarks>
internal sealed class IsolatedAnalyzerReference(
    IsolatedAnalyzerReferenceSet isolatedAnalyzerReferenceSet,
    AnalyzerFileReference underlyingAnalyzerReference)
    : AnalyzerReference
{
    /// <summary>
    /// Conditional weak tables that ensure that as long as a particular <see cref="DiagnosticAnalyzer"/> or <see
    /// cref="ISourceGenerator"/> is alive, that the corresponding <see cref="IsolatedAnalyzerReferenceSet"/> (and its
    /// corresponding <see cref="AssemblyLoadContext"/> is kept alive.
    /// </summary>
    private static readonly ConditionalWeakTable<DiagnosticAnalyzer, IsolatedAnalyzerReferenceSet> s_analyzerToPinnedReferenceSet = [];

    /// <inheritdoc cref="s_analyzerToPinnedReferenceSet"/>
    private static readonly ConditionalWeakTable<ISourceGenerator, IsolatedAnalyzerReferenceSet> s_generatorToPinnedReferenceSet = [];

    /// <summary>
    /// We keep a strong reference here.  As long as this <see cref="IsolatedAnalyzerReference"/> is passed out and held
    /// onto (say by a Project instance), it should keep the IsolatedAssemblyReferenceSet (and its ALC) alive.
    /// </summary>
    private readonly IsolatedAnalyzerReferenceSet _isolatedAnalyzerReferenceSet = isolatedAnalyzerReferenceSet;

    /// <summary>
    /// The actual real <see cref="AnalyzerReference"/> we defer our operations to.
    /// </summary>
    public readonly AnalyzerFileReference UnderlyingAnalyzerReference = underlyingAnalyzerReference;

    public override string Display => UnderlyingAnalyzerReference.Display;
    public override string? FullPath => UnderlyingAnalyzerReference.FullPath;
    public override object Id => UnderlyingAnalyzerReference.Id;

    public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language)
        => PinAnalyzers(static (reference, language) => reference.GetAnalyzers(language), language);

    public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages()
        => PinAnalyzers(static (reference, _) => reference.GetAnalyzersForAllLanguages(), default(VoidResult));

    [Obsolete]
    public override ImmutableArray<ISourceGenerator> GetGenerators()
        => PinGenerators(static (reference, _) => reference.GetGenerators(), default(VoidResult));

    public override ImmutableArray<ISourceGenerator> GetGenerators(string language)
        => PinGenerators(static (reference, language) => reference.GetGenerators(language), language);

    public override ImmutableArray<ISourceGenerator> GetGeneratorsForAllLanguages()
        => PinGenerators(static (reference, _) => reference.GetGeneratorsForAllLanguages(), default(VoidResult));

    private ImmutableArray<DiagnosticAnalyzer> PinAnalyzers<TArg>(Func<AnalyzerReference, TArg, ImmutableArray<DiagnosticAnalyzer>> getItems, TArg arg)
        => PinItems(s_analyzerToPinnedReferenceSet, getItems, arg);

    private ImmutableArray<ISourceGenerator> PinGenerators<TArg>(Func<AnalyzerReference, TArg, ImmutableArray<ISourceGenerator>> getItems, TArg arg)
        => PinItems(s_generatorToPinnedReferenceSet, getItems, arg);

    private ImmutableArray<TItem> PinItems<TItem, TArg>(
        ConditionalWeakTable<TItem, IsolatedAnalyzerReferenceSet> table,
        Func<AnalyzerReference, TArg, ImmutableArray<TItem>> getItems,
        TArg arg)
        where TItem : class
    {
        // Keep a reference from each generator to the IsolatedAssemblyReferenceSet.  This will ensure it (and the ALC
        // it points at) stays alive as long as the generator instance stays alive.
        var items = getItems(this.UnderlyingAnalyzerReference, arg);

        foreach (var item in items)
            table.TryAdd(item, _isolatedAnalyzerReferenceSet);

        // Note: we want to keep ourselves alive during this call so that neither we nor our reference set get GC'ed
        // while we're computing the items.
        GC.KeepAlive(this);

        return items;
    }

    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj);

    public override int GetHashCode()
        => RuntimeHelpers.GetHashCode(this);

    public override string ToString()
        => $"{nameof(IsolatedAnalyzerReference)}({UnderlyingAnalyzerReference})";
}

#endif
