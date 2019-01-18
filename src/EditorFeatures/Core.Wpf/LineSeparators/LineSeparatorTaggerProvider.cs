// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.LineSeparators
{
    /// <summary>
    /// This factory is called to create taggers that provide information about where line
    /// separators go.
    /// </summary>
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(LineSeparatorTag))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    internal partial class LineSeparatorTaggerProvider : AsynchronousTaggerProvider<LineSeparatorTag>
    {
        private readonly IEditorFormatMap _editorFormatMap;

        protected override IEnumerable<PerLanguageOption<bool>> PerLanguageOptions => SpecializedCollections.SingletonEnumerable(FeatureOnOffOptions.LineSeparator);

        private readonly object _lineSeperatorTagGate = new object();
        private LineSeparatorTag _lineSeparatorTag;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LineSeparatorTaggerProvider(
            IThreadingContext threadingContext,
            IEditorFormatMapService editorFormatMapService,
            IForegroundNotificationService notificationService,
            IAsynchronousOperationListenerProvider listenerProvider)
                : base(threadingContext, listenerProvider.GetListener(FeatureAttribute.LineSeparators), notificationService)
        {
            _editorFormatMap = editorFormatMapService.GetEditorFormatMap("text");
            _editorFormatMap.FormatMappingChanged += OnFormatMappingChanged;
            _lineSeparatorTag = new LineSeparatorTag(_editorFormatMap);
        }

        private void OnFormatMappingChanged(object sender, FormatItemsEventArgs e)
        {
            lock (_lineSeperatorTagGate)
            {
                _lineSeparatorTag = new LineSeparatorTag(_editorFormatMap);
            }
        }

        protected override ITaggerEventSource CreateEventSource(
            ITextView textView, ITextBuffer subjectBuffer)
        {
            return TaggerEventSources.Compose(
                new EditorFormatMapChangedEventSource(_editorFormatMap, TaggerDelay.NearImmediate),
                TaggerEventSources.OnTextChanged(subjectBuffer, TaggerDelay.NearImmediate));
        }

        protected override async Task ProduceTagsAsync(TaggerContext<LineSeparatorTag> context, DocumentSnapshotSpan documentSnapshotSpan, int? caretPosition)
        {
            var cancellationToken = context.CancellationToken;
            var document = documentSnapshotSpan.Document;
            if (document == null)
            {
                return;
            }

            var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            if (!documentOptions.GetOption(FeatureOnOffOptions.LineSeparator))
            {
                return;
            }

            LineSeparatorTag tag;
            lock (_lineSeperatorTagGate)
            {
                tag = _lineSeparatorTag;
            }

            using (Logger.LogBlock(FunctionId.Tagger_LineSeparator_TagProducer_ProduceTags, cancellationToken))
            {
                var snapshotSpan = documentSnapshotSpan.SnapshotSpan;
                var lineSeparatorService = document.GetLanguageService<ILineSeparatorService>();
                var lineSeparatorSpans = await lineSeparatorService.GetLineSeparatorsAsync(document, snapshotSpan.Span.ToTextSpan(), cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var span in lineSeparatorSpans)
                {
                    context.AddTag(new TagSpan<LineSeparatorTag>(span.ToSnapshotSpan(snapshotSpan.Snapshot), tag));
                }
            }
        }
    }
}
