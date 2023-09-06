// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Workspaces;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal static class DiagnosticTaggingOptionsStorage
{
    public static readonly Option2<bool> PullDiagnosticTagging = new(
        "dotnet_pull_diagnostic_tagging", defaultValue: true);
}

/// <summary>
/// Base type of all diagnostic taggers (classification, squiggles, suggestions, inline-diags).  Subclasses can control
/// things by overriding functionality in this type.  Internally, this will switch to either a pull or push cased
/// approach at instantiation time depending on our internal feature flag.
/// </summary>
internal abstract partial class AbstractPushOrPullDiagnosticsTaggerProvider<TTag> : ITaggerProvider
    where TTag : ITag
{
    private readonly ITaggerProvider _underlyingTaggerProvider;

    protected readonly IGlobalOptionService GlobalOptions;

    protected AbstractPushOrPullDiagnosticsTaggerProvider(
        IThreadingContext threadingContext,
        IDiagnosticService diagnosticService,
        IDiagnosticAnalyzerService analyzerService,
        IGlobalOptionService globalOptions,
        ITextBufferVisibilityTracker? visibilityTracker,
        IAsynchronousOperationListener listener)
    {
        GlobalOptions = globalOptions;

        // We make an up front check if tagging itself is in 'pull' mode (directly using snapshots and calling through
        // IDiagnosticAnalyzerService) or in 'push' mode (listening to events from IDiagnosticService and trying to map
        // them to the current document snapshot).  Note that this flag is independent of the flag to determine if LSP
        // pull diagnostics is on or not.  We support the following combinations:
        //
        //  Diagnostic Mode | Tagging Mode | Classification | Squiggles   | Suggestions | Inline Diagnostics
        //  ------------------------------------------------------------------------------------------------
        //  Pull            | Pull         | LSP            | LSP         | LSP         | Pull Tagger
        //  Pull            | Push         | LSP            | LSP         | LSP         | Push Tagger
        //  Push            | Pull         | Pull Tagger    | Pull Tagger | Pull Tagger | Pull Tagger
        //  Push            | Push         | Push Tagger    | Push Tagger | Push Tagger | Push Tagger
        //
        // Put another way, if DiagnosticMode is 'Push' (non-LSP), then this type does all the work, choosing tagging
        // pull/push for all features.   If DiagnosticMode is 'pull' (LSP), then LSP takes over classification,
        // squiggles, and suggestions, while we still handle inline-diagnostics.
        if (globalOptions.GetOption(DiagnosticTaggingOptionsStorage.PullDiagnosticTagging))
        {
            _underlyingTaggerProvider = new PullDiagnosticsTaggerProvider(
                this, threadingContext, diagnosticService, analyzerService, globalOptions, visibilityTracker, listener);
        }
        else
        {
            _underlyingTaggerProvider = new PushDiagnosticsTaggerProvider(
                this, threadingContext, diagnosticService, globalOptions, visibilityTracker, listener);
        }
    }

    public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        => _underlyingTaggerProvider.CreateTagger<T>(buffer);

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

    // Functionality for subclasses to control how this diagnostic tagging operates.  All the individual
    // SingleDiagnosticKindTaggerProvider will defer to these to do the work so that they otherwise operate
    // identically.

    protected abstract ImmutableArray<IOption2> Options { get; }
    protected virtual ImmutableArray<IOption2> FeatureOptions { get; } = ImmutableArray<IOption2>.Empty;

    protected abstract bool IsEnabled { get; }

    protected abstract bool SupportsDiagnosticMode(DiagnosticMode mode);
    protected abstract bool IncludeDiagnostic(DiagnosticData data);

    protected abstract bool TagEquals(TTag tag1, TTag tag2);
    protected abstract ITagSpan<TTag>? CreateTagSpan(Workspace workspace, bool isLiveUpdate, SnapshotSpan span, DiagnosticData data);

    /// <summary>
    /// Get the <see cref="DiagnosticDataLocation"/> that should have the tag applied to it.
    /// In most cases, this is the <see cref="DiagnosticData.DataLocation"/> but overrides can change it (e.g. unnecessary classifications).
    /// </summary>
    /// <param name="diagnosticData">the diagnostic containing the location(s).</param>
    /// <returns>an array of locations that should have the tag applied.</returns>
    protected virtual ImmutableArray<DiagnosticDataLocation> GetLocationsToTag(DiagnosticData diagnosticData)
        => diagnosticData.DataLocation is not null ? ImmutableArray.Create(diagnosticData.DataLocation) : ImmutableArray<DiagnosticDataLocation>.Empty;
}
