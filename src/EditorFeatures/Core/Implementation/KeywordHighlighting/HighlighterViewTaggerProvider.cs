// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
{
    using Context = AsynchronousTaggerContext<HighlightTag>;

    [Export(typeof(IViewTaggerProvider))]
    [TagType(typeof(HighlightTag))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal class HighlighterViewTaggerProvider : AsynchronousViewTaggerProvider<HighlightTag>
    {
        private readonly IHighlightingService _highlightingService;

        // Whenever any text change happens, we want to immediately remove any highlights that 
        // touch the edit.
        public override TaggerTextChangeBehavior TextChangeBehavior => TaggerTextChangeBehavior.RemoveTagsThatIntersectEdits;
        public override IEnumerable<Option<bool>> Options => SpecializedCollections.SingletonEnumerable(InternalFeatureOnOffOptions.KeywordHighlight);

        [ImportingConstructor]
        public HighlighterViewTaggerProvider(
            IHighlightingService highlightingService,
            IForegroundNotificationService notificationService,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
            : base(new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.KeywordHighlighting), notificationService)
        {
            _highlightingService = highlightingService;
        }

        public override ITaggerEventSource CreateEventSource(ITextView textView, ITextBuffer subjectBuffer)
        {
            return TaggerEventSources.Compose(
                TaggerEventSources.OnTextChanged(subjectBuffer, TaggerDelay.OnIdle),
                TaggerEventSources.OnCaretPositionChanged(textView, subjectBuffer, TaggerDelay.NearImmediate),
                TaggerEventSources.OnOptionChanged(subjectBuffer, FeatureOnOffOptions.KeywordHighlighting, TaggerDelay.NearImmediate));
        }

        // Internal for testing purposes
        public override async Task ProduceTagsAsync(Context context, DocumentSnapshotSpan documentSnapshotSpan, int? caretPosition)
        {
            var cancellationToken = context.CancellationToken;
            var document = documentSnapshotSpan.Document;
            if (document == null)
            {
                return;
            }

            var options = document.Project.Solution.Workspace.Options;
            if (!options.GetOption(FeatureOnOffOptions.KeywordHighlighting, document.Project.Language))
            {
                return;
            }

            if (caretPosition.HasValue)
            {
                var snapshotSpan = documentSnapshotSpan.SnapshotSpan;
                var position = caretPosition.Value;
                var snapshot = snapshotSpan.Snapshot;

                using (Logger.LogBlock(FunctionId.Tagger_Highlighter_TagProducer_ProduceTags, cancellationToken))
                {
                    var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                    var spans = _highlightingService.GetHighlights(root, position, cancellationToken);
                    foreach (var span in spans)
                    {
                        context.AddTag(new TagSpan<HighlightTag>(span.ToSnapshotSpan(snapshot), HighlightTag.Instance));
                    }
                }
            }
        }
    }
}