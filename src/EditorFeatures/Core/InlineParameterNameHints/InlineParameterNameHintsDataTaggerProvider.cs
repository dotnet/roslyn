// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InlineParameterNameHints;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.InlineParameterNameHints
{
    /// <summary>
    /// The TaggerProvider that calls upon the service in order to locate the spans and names
    /// </summary>
    [Export(typeof(ITaggerProvider))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [TagType(typeof(InlineParameterNameHintDataTag))]
    [Name(nameof(InlineParameterNameHintsDataTaggerProvider))]
    internal class InlineParameterNameHintsDataTaggerProvider : AsynchronousTaggerProvider<InlineParameterNameHintDataTag>
    {
        private readonly IAsynchronousOperationListener _listener;
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        [ImportingConstructor]
        public InlineParameterNameHintsDataTaggerProvider(
            IThreadingContext threadingContext,
            IAsynchronousOperationListenerProvider listenerProvider,
            IForegroundNotificationService notificationService)
            : base(threadingContext, listenerProvider.GetListener(FeatureAttribute.InlineParameterNameHints), notificationService)
        {
            _listener = listenerProvider.GetListener(FeatureAttribute.InlineParameterNameHints);
        }

        protected override ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            // TaggerDelay is NearImmediate because we want the renaming and tag creation to be instantaneous
            return TaggerEventSources.OnWorkspaceChanged(subjectBuffer, TaggerDelay.NearImmediate, _listener);
        }

        protected override async Task ProduceTagsAsync(TaggerContext<InlineParameterNameHintDataTag> context, DocumentSnapshotSpan documentSnapshotSpan, int? caretPosition)
        {
            var cancellationToken = context.CancellationToken;
            var document = documentSnapshotSpan.Document;

            var snapshotSpan = documentSnapshotSpan.SnapshotSpan;
            var paramNameHintsService = document.GetRequiredLanguageService<IInlineParameterNameHintsService>();
            var paramNameHintSpans = await paramNameHintsService.GetInlineParameterNameHintsAsync(document, snapshotSpan.Span.ToTextSpan(), cancellationToken).ConfigureAwait(false);

            foreach (var span in paramNameHintSpans)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                context.AddTag(new TagSpan<InlineParameterNameHintDataTag>(new SnapshotSpan(snapshotSpan.Snapshot, span.Position, 0), new InlineParameterNameHintDataTag(span.Name)));
            }
        }
    }
}
