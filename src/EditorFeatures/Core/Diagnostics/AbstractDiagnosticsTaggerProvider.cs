// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Workspaces;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Base type of all diagnostic taggers (classification, squiggles, suggestions, inline-diags).  Subclasses can control
/// things by overriding functionality in this type.  Internally, this will switch to either a pull or push cased
/// approach at instantiation time depending on our internal feature flag.
/// </summary>
internal abstract partial class AbstractDiagnosticsTaggerProvider<TTag> : ITaggerProvider
    where TTag : ITag
{
    protected readonly IGlobalOptionService GlobalOptions;

    /// <summary>
    /// Underlying diagnostic tagger responsible for the syntax/semantic and compiler/analyzer split.  The ordering of
    /// these taggers is not relevant.  They are not executed serially.  Rather, they all run concurrently, notifying us
    /// (potentially concurrently as well) when change occur.
    /// </summary>
    private readonly ImmutableArray<SingleDiagnosticKindPullTaggerProvider> _diagnosticsTaggerProviders;

    public AbstractDiagnosticsTaggerProvider(
        IThreadingContext threadingContext,
        IDiagnosticService diagnosticService,
        IDiagnosticAnalyzerService analyzerService,
        IGlobalOptionService globalOptions,
        ITextBufferVisibilityTracker? visibilityTracker,
        IAsynchronousOperationListener listener)
    {
        GlobalOptions = globalOptions;

        _diagnosticsTaggerProviders = ImmutableArray.Create(
            CreateDiagnosticsTaggerProvider(DiagnosticKind.CompilerSyntax),
            CreateDiagnosticsTaggerProvider(DiagnosticKind.CompilerSemantic),
            CreateDiagnosticsTaggerProvider(DiagnosticKind.AnalyzerSyntax),
            CreateDiagnosticsTaggerProvider(DiagnosticKind.AnalyzerSemantic));

        return;

        SingleDiagnosticKindPullTaggerProvider CreateDiagnosticsTaggerProvider(DiagnosticKind diagnosticKind)
            => new(this, diagnosticKind, threadingContext, diagnosticService, analyzerService, globalOptions, visibilityTracker, listener);
    }

    // Functionality for subclasses to control how this diagnostic tagging operates.  All the individual
    // SingleDiagnosticKindTaggerProvider will defer to these to do the work so that they otherwise operate
    // identically.

    protected abstract ImmutableArray<IOption2> Options { get; }
    protected virtual ImmutableArray<IOption2> FeatureOptions { get; } = ImmutableArray<IOption2>.Empty;

    protected abstract bool IsEnabled { get; }

    protected abstract bool SupportsDiagnosticMode(DiagnosticMode mode);
    protected abstract bool IncludeDiagnostic(DiagnosticData data);

    protected abstract bool TagEquals(TTag tag1, TTag tag2);
    protected abstract ITagSpan<TTag>? CreateTagSpan(Workspace workspace, SnapshotSpan span, DiagnosticData data);

    /// <summary>
    /// Get the <see cref="DiagnosticDataLocation"/> that should have the tag applied to it.
    /// In most cases, this is the <see cref="DiagnosticData.DataLocation"/> but overrides can change it (e.g. unnecessary classifications).
    /// </summary>
    /// <param name="diagnosticData">the diagnostic containing the location(s).</param>
    /// <returns>an array of locations that should have the tag applied.</returns>
    protected virtual ImmutableArray<DiagnosticDataLocation> GetLocationsToTag(DiagnosticData diagnosticData)
        => diagnosticData.DataLocation is not null ? ImmutableArray.Create(diagnosticData.DataLocation) : ImmutableArray<DiagnosticDataLocation>.Empty;

    public ITagger<T>? CreateTagger<T>(ITextBuffer buffer) where T : ITag
    {
        using var taggers = TemporaryArray<EfficientTagger<TTag>>.Empty;
        foreach (var taggerProvider in _diagnosticsTaggerProviders)
        {
            var innerTagger = taggerProvider.CreateTagger(buffer);
            if (innerTagger != null)
                taggers.Add(innerTagger);
        }

        var tagger = new SimpleAggregateTagger<TTag>(taggers.ToImmutableAndClear());
        if (tagger is not ITagger<T> genericTagger)
        {
            tagger.Dispose();
            return null;
        }

        return genericTagger;
    }

    private static ITaggerEventSource CreateEventSourceWorker(ITextBuffer subjectBuffer, IDiagnosticService diagnosticService)
    {
        // OnTextChanged is added for diagnostics in source generated files: it's possible that the analyzer driver
        // executed on content which was produced by a source generator but is not yet reflected in an open text
        // buffer for that generated file. In this case, we need to update the tags after the buffer updates (which
        // triggers a text changed event) to ensure diagnostics are positioned correctly.
        return TaggerEventSources.Compose(
            TaggerEventSources.OnDocumentActiveContextChanged(subjectBuffer),
            TaggerEventSources.OnWorkspaceRegistrationChanged(subjectBuffer),
            TaggerEventSources.OnDiagnosticsChanged(subjectBuffer, diagnosticService),
            TaggerEventSources.OnTextChanged(subjectBuffer));
    }
}
