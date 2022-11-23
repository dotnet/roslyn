// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Workspaces;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Base type for all taggers that interact with the <see cref="IDiagnosticAnalyzerService"/> and produce tags for the
/// diagnostics with different UI presentations.  It does no computation work itself, but instead defers that to it's
/// underlying <see cref="AsynchronousDiagnosticsTaggerProvider{TTag}"/>s.
/// </summary>
internal abstract partial class AbstractAggregateDiagnosticsTaggerProvider<TTag>
    : IViewTaggerProvider, AsynchronousDiagnosticsTaggerProvider<TTag>.ICallback
    where TTag : ITag
{
    /// <summary>
    /// Underlying diagnostic tagger responsible for the syntax/semantic and compiler/analyzer split.
    /// </summary>
    private readonly ImmutableArray<AsynchronousDiagnosticsTaggerProvider<TTag>> _diagnosticsTaggerProviders;

    protected readonly IGlobalOptionService GlobalOptions;

    protected AbstractAggregateDiagnosticsTaggerProvider(
        IThreadingContext threadingContext,
        IDiagnosticService diagnosticService,
        IDiagnosticAnalyzerService analyzerService,
        IGlobalOptionService globalOptions,
        ITextBufferVisibilityTracker? visibilityTracker,
        IAsynchronousOperationListener listener)
    {
        GlobalOptions = globalOptions;

        _diagnosticsTaggerProviders = ImmutableArray.Create(
            CreateDiagnosticsTaggerProvider(DiagnosticKinds.CompilerSyntax),
            CreateDiagnosticsTaggerProvider(DiagnosticKinds.CompilerSemantic),
            CreateDiagnosticsTaggerProvider(DiagnosticKinds.AnalyzerSyntax),
            CreateDiagnosticsTaggerProvider(DiagnosticKinds.AnalyzerSemantic));

        return;

        AsynchronousDiagnosticsTaggerProvider<TTag> CreateDiagnosticsTaggerProvider(DiagnosticKinds diagnosticKinds)
            => new(this, diagnosticKinds, threadingContext, diagnosticService, analyzerService, globalOptions, visibilityTracker, listener);
    }

    public ITagger<T>? CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
    {
        using var _ = ArrayBuilder<ITagger<TTag>>.GetInstance(out var taggers);
        foreach (var taggerProvider in _diagnosticsTaggerProviders)
            taggers.AddIfNotNull(taggerProvider.CreateTagger<TTag>(textView, buffer));

        var tagger = new AggregateTagger(this, taggers.ToImmutable());
        if (tagger is not ITagger<T> genericTagger)
        {
            tagger.Dispose();
            return null;
        }

        return genericTagger;
    }

    #region AsynchronousDiagnosticsTaggerProvider<TTag>.ICallback

    public abstract ImmutableArray<IOption> Options { get; }
    public virtual ImmutableArray<IOption> FeatureOptions { get; } = ImmutableArray<IOption>.Empty;

    public abstract bool IsEnabled { get; }

    public abstract bool SupportsDiagnosticMode(DiagnosticMode mode);
    public abstract bool IncludeDiagnostic(DiagnosticData data);

    public abstract bool TagEquals(TTag tag1, TTag tag2);
    public abstract ITagSpan<TTag>? CreateTagSpan(Workspace workspace, SnapshotSpan span, DiagnosticData data);

    /// <summary>
    /// Get the <see cref="DiagnosticDataLocation"/> that should have the tag applied to it.
    /// In most cases, this is the <see cref="DiagnosticData.DataLocation"/> but overrides can change it (e.g. unnecessary classifications).
    /// </summary>
    /// <param name="diagnosticData">the diagnostic containing the location(s).</param>
    /// <returns>an array of locations that should have the tag applied.</returns>
    public virtual ImmutableArray<DiagnosticDataLocation> GetLocationsToTag(DiagnosticData diagnosticData)
        => diagnosticData.DataLocation is not null ? ImmutableArray.Create(diagnosticData.DataLocation) : ImmutableArray<DiagnosticDataLocation>.Empty;

    #endregion
}
