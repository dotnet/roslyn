// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Classification;

/// <summary>
/// This is the tagger we use for view classification scenarios.  It is used for classifying code
/// in the editor.  We use a view tagger so that we can only classify what's in view, and not
/// the whole file.
/// </summary>
internal abstract class AbstractSemanticOrEmbeddedClassificationViewTaggerProvider(
    TaggerHost taggerHost,
    ClassificationTypeMap typeMap,
    ClassificationType type)
    : AsynchronousViewportTaggerProvider<IClassificationTag>(taggerHost, FeatureAttribute.Classification)
{
    private readonly ClassificationTypeMap _typeMap = typeMap;
    private readonly ClassificationType _type = type;

    protected sealed override ImmutableArray<IOption2> Options { get; } = [SemanticColorizerOptionsStorage.SemanticColorizer];

    protected sealed override TaggerDelay EventChangeDelay => TaggerDelay.Short;

    /// <summary>
    /// We do classification in two passes.  In the first pass we do not block getting classifications on building the
    /// full compilation.  This may take a significant amount of time and can cause a very latency sensitive operation
    /// (copying) to block the user while we wait on this work to happen.  
    /// <para>
    /// It's also a better experience to get classifications to the user faster versus waiting a potentially large
    /// amount of time waiting for all the compilation information to be built.  For example, we can classify types that
    /// we've parsed in other files, or partially loaded from metadata, even if we're still parsing/loading. For cross
    /// language projects, this also produces semantic classifications more quickly as we do not have to wait on
    /// skeletons to be built.
    /// </para>
    /// <para>
    /// In the second pass though, we will go and do things without frozen-partial semantics, so that we do always snap
    /// to a final correct state.  Note: the expensive second pass will be kicked down the road as new events come in to
    /// classify things.
    /// </para>
    /// </summary>
    protected sealed override bool SupportsFrozenPartialSemantics => true;

    protected override bool TagEquals(IClassificationTag tag1, IClassificationTag tag2)
        => tag1.ClassificationType.Classification == tag2.ClassificationType.Classification;

    protected sealed override ITaggerEventSource CreateEventSource(ITextView textView, ITextBuffer subjectBuffer)
    {
        this.ThreadingContext.ThrowIfNotOnUIThread();

        // Note: we don't listen for OnTextChanged.  They'll get reported by the ViewSpan changing and also the
        // SemanticChange notification. 
        return TaggerEventSources.Compose(
            TaggerEventSources.OnViewSpanChanged(ThreadingContext, textView),
            TaggerEventSources.OnWorkspaceChanged(subjectBuffer, AsyncListener),
            TaggerEventSources.OnDocumentActiveContextChanged(subjectBuffer),
            TaggerEventSources.OnGlobalOptionChanged(GlobalOptions,
                static option => option.Equals(ClassificationOptionsStorage.ClassifyReassignedVariables) || option.Equals(ClassificationOptionsStorage.ClassifyObsoleteSymbols)));
    }

    protected sealed override async Task ProduceTagsAsync(
        TaggerContext<IClassificationTag> context, DocumentSnapshotSpan spanToTag, CancellationToken cancellationToken)
    {
        var document = spanToTag.Document;
        if (document == null)
            return;

        // Attempt to get a classification service which will actually produce the results.
        // If we can't (because we have no Document, or because the language doesn't support
        // this service), then bail out immediately.
        var classificationService = document.GetLanguageService<IClassificationService>();
        if (classificationService == null)
            return;

        // The LSP client will handle producing tags when running under the LSP editor.
        // Our tagger implementation should return nothing to prevent conflicts.
        var workspaceContextService = document.Project.Solution.Services.GetRequiredService<IWorkspaceContextService>();
        if (workspaceContextService?.IsInLspEditorContext() == true)
            return;

        // If the LSP semantic tokens feature flag is enabled, return nothing to prevent conflicts.
        var isLspSemanticTokensEnabled = this.GlobalOptions.GetOption(LspOptionsStorage.LspSemanticTokensFeatureFlag);
        if (isLspSemanticTokensEnabled)
            return;

        var classificationOptions = this.GlobalOptions.GetClassificationOptions(document.Project.Language);
        await ProduceTagsAsync(
            context, spanToTag, classificationService, classificationOptions, cancellationToken).ConfigureAwait(false);
    }

    public async Task ProduceTagsAsync(
        TaggerContext<IClassificationTag> context,
        DocumentSnapshotSpan spanToTag,
        IClassificationService classificationService,
        ClassificationOptions options,
        CancellationToken cancellationToken)
    {
        var document = spanToTag.Document;
        if (document == null)
            return;

        var currentSemanticVersion = await document.Project.GetDependentSemanticVersionAsync(cancellationToken).ConfigureAwait(false);
        var classified = await TryClassifyContainingMemberSpanAsync(
            context, document, spanToTag.SnapshotSpan, classificationService, options, currentSemanticVersion, cancellationToken).ConfigureAwait(false);
        if (classified)
        {
            return;
        }

        // We weren't able to use our specialized codepaths for semantic classifying. 
        // Fall back to classifying the full span that was asked for.
        await ClassifySpansAsync(
            context, document, spanToTag.SnapshotSpan, classificationService, options, currentSemanticVersion, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> TryClassifyContainingMemberSpanAsync(
        TaggerContext<IClassificationTag> context,
        Document document,
        SnapshotSpan snapshotSpan,
        IClassificationService classificationService,
        ClassificationOptions options,
        VersionStamp currentSemanticVersion,
        CancellationToken cancellationToken)
    {
        // there was top level edit, check whether that edit updated top level element
        if (!document.SupportsSyntaxTree)
            return false;

        // No cached state, so we can't check if the edits were just inside a member.
        if (context.State is null)
            return false;

        // Retrieve the information about the last time we classified this document.
        var (lastSemanticVersion, lastTextImageVersion) = ((VersionStamp, ITextImageVersion))context.State;

        // if a top level change was made.  We can't perform this optimization.
        if (lastSemanticVersion != currentSemanticVersion)
            return false;

        var service = document.GetRequiredLanguageService<ISyntaxFactsService>();

        // perf optimization. Check whether all edits since the last update has happened within a member. If it did, it
        // will find the member that contains the changes and only refresh that member.  If possible, try to get a
        // speculative binder to make things even cheaper.

        var currentTextImageVersion = GetTextImageVersion(snapshotSpan);

        var textChangeRanges = ITextImageHelpers.GetChangeRanges(lastTextImageVersion, currentTextImageVersion);
        var collapsedRange = TextChangeRange.Collapse(textChangeRanges);

        var changedSpan = new TextSpan(collapsedRange.Span.Start, collapsedRange.NewLength);

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var member = service.GetContainingMemberDeclaration(root, changedSpan.Start);
        if (member == null || !member.FullSpan.Contains(changedSpan))
        {
            // The edit was not fully contained in a member.  Reclassify everything.
            return false;
        }

        var memberBodySpan = service.GetMemberBodySpanForSpeculativeBinding(member);
        if (memberBodySpan.IsEmpty)
        {
            // Wasn't a member we could reclassify independently.
            return false;
        }

        // TODO(cyrusn): Unclear what this logic is for.  It looks like it's just trying to narrow the span down
        // slightly from the full member, just to its body.  Unclear if this provides any substantive benefits. But
        // keeping for now to preserve long standing logic.
        var memberSpanToClassify = memberBodySpan.Contains(changedSpan)
            ? memberBodySpan.ToSpan()
            : member.FullSpan.ToSpan();

        // Take the subspan we know we want to classify, and intersect that with the actual span being asked for.
        // That way if we're only asking for a portion of a method, we still only classify that, and not the whole
        // method.
        var finalSpanToClassify = memberSpanToClassify.Intersection(snapshotSpan.Span);
        if (finalSpanToClassify is null)
            return false;

        var subSpanToTag = new SnapshotSpan(snapshotSpan.Snapshot, finalSpanToClassify.Value);

        // re-classify only the member we're inside.
        await ClassifySpansAsync(
            context, document, subSpanToTag, classificationService, options, currentSemanticVersion, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static ITextImageVersion GetTextImageVersion(SnapshotSpan snapshotSpan)
        => ((ITextSnapshot2)snapshotSpan.Snapshot).TextImage.Version;

    private async Task ClassifySpansAsync(
        TaggerContext<IClassificationTag> context,
        Document document,
        SnapshotSpan snapshotSpan,
        IClassificationService classificationService,
        ClassificationOptions options,
        VersionStamp currentSemanticVersion,
        CancellationToken cancellationToken)
    {
        try
        {
            using (Logger.LogBlock(FunctionId.Tagger_SemanticClassification_TagProducer_ProduceTags, cancellationToken))
            {
                using var _ = Classifier.GetPooledList(out var classifiedSpans);

                // Ensure that if we're producing tags for frozen/partial documents, that we pass along that info so
                // that we preserve that same behavior in OOP if we end up computing the tags there.
                options = options with { FrozenPartialSemantics = context.FrozenPartialSemantics };

                var span = snapshotSpan.Span;
                var snapshot = snapshotSpan.Snapshot;

                if (_type == ClassificationType.Semantic)
                {
                    await classificationService.AddSemanticClassificationsAsync(
                       document, span.ToTextSpan(), options, classifiedSpans, cancellationToken).ConfigureAwait(false);
                }
                else if (_type == ClassificationType.EmbeddedLanguage)
                {
                    await classificationService.AddEmbeddedLanguageClassificationsAsync(
                       document, span.ToTextSpan(), options, classifiedSpans, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    throw ExceptionUtilities.UnexpectedValue(_type);
                }

                foreach (var classifiedSpan in classifiedSpans)
                    context.AddTag(ClassificationUtilities.Convert(_typeMap, snapshot, classifiedSpan));

                // Let the context know that this was the span we actually tried to tag.
                context.SetSpansTagged([snapshotSpan]);

                // Store the semantic version and text-image-version we used to produce these tags.  We can use this in
                // the future to try to limit what we classify, if all edits were made within a single member.
                context.State = (currentSemanticVersion, GetTextImageVersion(snapshotSpan));
            }
        }
        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }
}
