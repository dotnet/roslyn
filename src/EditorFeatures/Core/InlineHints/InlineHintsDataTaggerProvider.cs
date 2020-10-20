// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
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
    internal class InlineHintsDataTaggerProvider : AsynchronousViewTaggerProvider<InlineHintDataTag>
    {
        private readonly IAsynchronousOperationListener _listener;

        protected override SpanTrackingMode SpanTrackingMode => SpanTrackingMode.EdgeInclusive;

        /// <summary>
        /// We want to make sure that if the user edits the space that the tag exists in that it goes away and they
        /// don't see stale tags sticking around in random locations until the next update.  A good example of when this
        /// is desirable is 'cut line'. If the tags aren't removed, then the line will be gone but the tags will remain
        /// at whatever points the tracking spans moved them to.
        /// </summary>
        protected override TaggerTextChangeBehavior TextChangeBehavior => TaggerTextChangeBehavior.RemoveTagsThatIntersectEdits;

        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        [ImportingConstructor]
        public InlineHintsDataTaggerProvider(
            IThreadingContext threadingContext,
            IAsynchronousOperationListenerProvider listenerProvider,
            IForegroundNotificationService notificationService)
            : base(threadingContext, listenerProvider.GetListener(FeatureAttribute.InlineParameterNameHints), notificationService)
        {
            _listener = listenerProvider.GetListener(FeatureAttribute.InlineParameterNameHints);
        }

        protected override ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            return TaggerEventSources.Compose(
                TaggerEventSources.OnViewSpanChanged(ThreadingContext, textViewOpt, textChangeDelay: TaggerDelay.Short, scrollChangeDelay: TaggerDelay.NearImmediate),
                TaggerEventSources.OnWorkspaceChanged(subjectBuffer, TaggerDelay.NearImmediate, _listener),
                TaggerEventSources.OnOptionChanged(subjectBuffer, InlineHintsOptions.DisplayAllOverride, TaggerDelay.NearImmediate),
                TaggerEventSources.OnOptionChanged(subjectBuffer, InlineHintsOptions.EnabledForParameters, TaggerDelay.NearImmediate),
                TaggerEventSources.OnOptionChanged(subjectBuffer, InlineHintsOptions.ForLiteralParameters, TaggerDelay.NearImmediate),
                TaggerEventSources.OnOptionChanged(subjectBuffer, InlineHintsOptions.ForObjectCreationParameters, TaggerDelay.NearImmediate),
                TaggerEventSources.OnOptionChanged(subjectBuffer, InlineHintsOptions.ForOtherParameters, TaggerDelay.NearImmediate),
                TaggerEventSources.OnOptionChanged(subjectBuffer, InlineHintsOptions.SuppressForParametersThatMatchMethodIntent, TaggerDelay.NearImmediate),
                TaggerEventSources.OnOptionChanged(subjectBuffer, InlineHintsOptions.SuppressForParametersThatDifferOnlyBySuffix, TaggerDelay.NearImmediate),
                TaggerEventSources.OnOptionChanged(subjectBuffer, InlineHintsOptions.EnabledForTypes, TaggerDelay.NearImmediate),
                TaggerEventSources.OnOptionChanged(subjectBuffer, InlineHintsOptions.ForImplicitVariableTypes, TaggerDelay.NearImmediate),
                TaggerEventSources.OnOptionChanged(subjectBuffer, InlineHintsOptions.ForLambdaParameterTypes, TaggerDelay.NearImmediate));
        }

        protected override IEnumerable<SnapshotSpan> GetSpansToTag(ITextView textView, ITextBuffer subjectBuffer)
        {
            this.AssertIsForeground();

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

        protected override async Task ProduceTagsAsync(TaggerContext<InlineHintDataTag> context, DocumentSnapshotSpan documentSnapshotSpan, int? caretPosition)
        {
            var cancellationToken = context.CancellationToken;
            var document = documentSnapshotSpan.Document;
            var service = document.GetLanguageService<IInlineHintsService>();
            if (service == null)
                return;

            var snapshotSpan = documentSnapshotSpan.SnapshotSpan;
            var hints = await service.GetInlineHintsAsync(document, snapshotSpan.Span.ToTextSpan(), cancellationToken).ConfigureAwait(false);
            foreach (var hint in hints)
            {
                // If we don't have any text to actually show the user, then don't make a tag.
                if (hint.DisplayParts.Sum(p => p.ToString().Length) == 0)
                    continue;

                context.AddTag(new TagSpan<InlineHintDataTag>(
                    hint.Span.ToSnapshotSpan(snapshotSpan.Snapshot),
                    new InlineHintDataTag(hint)));
            }
        }
    }
}
