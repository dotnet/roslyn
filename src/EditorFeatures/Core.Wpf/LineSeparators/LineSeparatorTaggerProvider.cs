// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.Tagging;
using Microsoft.CodeAnalysis.Editor.LineSeparators;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LineSeparators;
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

        protected override IEnumerable<PerLanguageOption2<bool>> PerLanguageOptions => SpecializedCollections.SingletonEnumerable(FeatureOnOffOptions.LineSeparator);

        private readonly object _lineSeperatorTagGate = new object();
        private LineSeparatorTag _lineSeparatorTag;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LineSeparatorTaggerProvider(
            IThreadingContext threadingContext,
            IEditorFormatMapService editorFormatMapService,
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(threadingContext, globalOptions, listenerProvider.GetListener(FeatureAttribute.LineSeparators))
        {
            _editorFormatMap = editorFormatMapService.GetEditorFormatMap("text");
            _editorFormatMap.FormatMappingChanged += OnFormatMappingChanged;
            _lineSeparatorTag = new LineSeparatorTag(_editorFormatMap);
        }

        protected override TaggerDelay EventChangeDelay => TaggerDelay.NearImmediate;

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
                new EditorFormatMapChangedEventSource(_editorFormatMap),
                TaggerEventSources.OnTextChanged(subjectBuffer));
        }

        protected override async Task ProduceTagsAsync(
            TaggerContext<LineSeparatorTag> context, DocumentSnapshotSpan documentSnapshotSpan, int? caretPosition, CancellationToken cancellationToken)
        {
            var document = documentSnapshotSpan.Document;
            if (document == null)
                return;

            if (!GlobalOptions.GetOption(FeatureOnOffOptions.LineSeparator, document.Project.Language))
                return;

            using (Logger.LogBlock(FunctionId.Tagger_LineSeparator_TagProducer_ProduceTags, cancellationToken))
            {
                var snapshotSpan = documentSnapshotSpan.SnapshotSpan;
                var lineSeparatorService = document.GetLanguageService<ILineSeparatorService>();
                if (lineSeparatorService == null)
                    return;

                var lineSeparatorSpans = await lineSeparatorService.GetLineSeparatorsAsync(document, snapshotSpan.Span.ToTextSpan(), cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                if (lineSeparatorSpans.Length == 0)
                    return;

                LineSeparatorTag tag;
                lock (_lineSeperatorTagGate)
                {
                    tag = _lineSeparatorTag;
                }

                foreach (var span in lineSeparatorSpans)
                    context.AddTag(new TagSpan<LineSeparatorTag>(span.ToSnapshotSpan(snapshotSpan.Snapshot), tag));
            }
        }
    }
}
