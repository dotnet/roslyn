// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.CodeAnalysis.Workspaces;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;
using VSUtilities = Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.InlineHints
{
    /// <summary>
    /// The TaggerProvider that calls upon the service in order to locate the spans and names
    /// </summary>
    [Export(typeof(IViewTaggerProvider))]
    [VSUtilities.ContentType(ContentTypeNames.RoslynContentType)]
    [TagType(typeof(InlineHintDataTag))]
    [VSUtilities.Name(nameof(InlineHintsDataTaggerProvider))]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    [method: ImportingConstructor]
    internal partial class InlineHintsDataTaggerProvider(
        IThreadingContext threadingContext,
        IGlobalOptionService globalOptions,
        [Import(AllowDefault = true)] IInlineHintKeyProcessor inlineHintKeyProcessor,
        [Import(AllowDefault = true)] ITextBufferVisibilityTracker? visibilityTracker,
        IAsynchronousOperationListenerProvider listenerProvider) : AsynchronousViewTaggerProvider<InlineHintDataTag>(threadingContext, globalOptions, visibilityTracker, listenerProvider.GetListener(FeatureAttribute.InlineHints))
    {
        private readonly IAsynchronousOperationListener _listener = listenerProvider.GetListener(FeatureAttribute.InlineHints);
        private readonly IInlineHintKeyProcessor _inlineHintKeyProcessor = inlineHintKeyProcessor;

        protected override SpanTrackingMode SpanTrackingMode => SpanTrackingMode.EdgeInclusive;

        /// <summary>
        /// We want to make sure that if the user edits the space that the tag exists in that it goes away and they
        /// don't see stale tags sticking around in random locations until the next update.  A good example of when this
        /// is desirable is 'cut line'. If the tags aren't removed, then the line will be gone but the tags will remain
        /// at whatever points the tracking spans moved them to.
        /// </summary>
        protected override TaggerTextChangeBehavior TextChangeBehavior => TaggerTextChangeBehavior.RemoveTagsThatIntersectEdits;

        protected override TaggerDelay EventChangeDelay => TaggerDelay.Short;

        protected override ITaggerEventSource CreateEventSource(ITextView textView, ITextBuffer subjectBuffer)
        {
            return TaggerEventSources.Compose(
                TaggerEventSources.OnViewSpanChanged(this.ThreadingContext, textView),
                TaggerEventSources.OnWorkspaceChanged(subjectBuffer, _listener),
                new InlineHintKeyProcessorEventSource(_inlineHintKeyProcessor),
                TaggerEventSources.OnGlobalOptionChanged(GlobalOptions, InlineHintsOptionsStorage.EnabledForParameters),
                TaggerEventSources.OnGlobalOptionChanged(GlobalOptions, InlineHintsOptionsStorage.ForLiteralParameters),
                TaggerEventSources.OnGlobalOptionChanged(GlobalOptions, InlineHintsOptionsStorage.ForIndexerParameters),
                TaggerEventSources.OnGlobalOptionChanged(GlobalOptions, InlineHintsOptionsStorage.ForObjectCreationParameters),
                TaggerEventSources.OnGlobalOptionChanged(GlobalOptions, InlineHintsOptionsStorage.ForOtherParameters),
                TaggerEventSources.OnGlobalOptionChanged(GlobalOptions, InlineHintsOptionsStorage.SuppressForParametersThatMatchMethodIntent),
                TaggerEventSources.OnGlobalOptionChanged(GlobalOptions, InlineHintsOptionsStorage.SuppressForParametersThatDifferOnlyBySuffix),
                TaggerEventSources.OnGlobalOptionChanged(GlobalOptions, InlineHintsOptionsStorage.SuppressForParametersThatMatchArgumentName),
                TaggerEventSources.OnGlobalOptionChanged(GlobalOptions, InlineHintsOptionsStorage.EnabledForTypes),
                TaggerEventSources.OnGlobalOptionChanged(GlobalOptions, InlineHintsOptionsStorage.ForImplicitVariableTypes),
                TaggerEventSources.OnGlobalOptionChanged(GlobalOptions, InlineHintsOptionsStorage.ForLambdaParameterTypes),
                TaggerEventSources.OnGlobalOptionChanged(GlobalOptions, InlineHintsOptionsStorage.ForImplicitObjectCreation));
        }

        protected override IEnumerable<SnapshotSpan> GetSpansToTag(ITextView? textView, ITextBuffer subjectBuffer)
        {
            this.ThreadingContext.ThrowIfNotOnUIThread();
            Contract.ThrowIfNull(textView);

            // Find the visible span some 100 lines +/- what's actually in view.  This way
            // if the user scrolls up/down, we'll already have the results.
            var visibleSpanOpt = textView.GetVisibleLinesSpan(subjectBuffer, extraLines: 100);
            if (visibleSpanOpt == null)
            {
                // Couldn't find anything visible, just fall back to tagging all hint locations
                return base.GetSpansToTag(textView, subjectBuffer);
            }

            return SpecializedCollections.SingletonEnumerable(visibleSpanOpt.Value);
        }

        protected override async Task ProduceTagsAsync(
            TaggerContext<InlineHintDataTag> context, DocumentSnapshotSpan documentSnapshotSpan, int? caretPosition, CancellationToken cancellationToken)
        {
            var document = documentSnapshotSpan.Document;
            if (document == null)
                return;

            var service = document.GetLanguageService<IInlineHintsService>();
            if (service == null)
                return;

            var options = GlobalOptions.GetInlineHintsOptions(document.Project.Language);

            var snapshotSpan = documentSnapshotSpan.SnapshotSpan;
            var hints = await service.GetInlineHintsAsync(
                document, snapshotSpan.Span.ToTextSpan(), options,
                displayAllOverride: _inlineHintKeyProcessor?.State is true,
                cancellationToken).ConfigureAwait(false);

            foreach (var hint in hints)
            {
                // If we don't have any text to actually show the user, then don't make a tag.
                if (hint.DisplayParts.Sum(p => p.ToString().Length) == 0)
                    continue;

                context.AddTag(new TagSpan<InlineHintDataTag>(
                    hint.Span.ToSnapshotSpan(snapshotSpan.Snapshot),
                    new InlineHintDataTag(this, snapshotSpan.Snapshot, hint)));
            }
        }

        protected override bool TagEquals(InlineHintDataTag tag1, InlineHintDataTag tag2)
            => tag1.Equals(tag2);
    }
}
