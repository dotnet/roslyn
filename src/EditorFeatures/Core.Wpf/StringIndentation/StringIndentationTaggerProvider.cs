// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.StringIndentation;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.StringIndentation
{
    /// <summary>
    /// This factory is called to create taggers that provide information about where line
    /// separators go.
    /// </summary>
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(StringIndentationTag))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    internal partial class LineSeparatorTaggerProvider : AsynchronousTaggerProvider<StringIndentationTag>
    {
        private readonly IEditorFormatMap _editorFormatMap;

        protected override IEnumerable<PerLanguageOption2<bool>> PerLanguageOptions => SpecializedCollections.SingletonEnumerable(FeatureOnOffOptions.StringIdentation);

        private readonly object _lineSeperatorTagGate = new();
        private StringIndentationTag _lineSeparatorTag;

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
            _lineSeparatorTag = new StringIndentationTag(_editorFormatMap);
        }

        protected override TaggerDelay EventChangeDelay => TaggerDelay.NearImmediate;

        private void OnFormatMappingChanged(object sender, FormatItemsEventArgs e)
        {
            lock (_lineSeperatorTagGate)
            {
                _lineSeparatorTag = new StringIndentationTag(_editorFormatMap);
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
            TaggerContext<StringIndentationTag> context, DocumentSnapshotSpan documentSnapshotSpan, int? caretPosition, CancellationToken cancellationToken)
        {
            var document = documentSnapshotSpan.Document;
            if (document == null)
                return;

            if (!GlobalOptions.GetOption(FeatureOnOffOptions.StringIdentation, document.Project.Language))
                return;

            var service = document.GetLanguageService<IStringIndentationService>();
            if (service == null)
                return;

            var snapshotSpan = documentSnapshotSpan.SnapshotSpan;
            var spans = await service.GetStringIndentationSpansAsync(document, snapshotSpan.Span.ToTextSpan(), cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (spans.Length == 0)
                return;

            StringIndentationTag tag;
            lock (_lineSeperatorTagGate)
            {
                tag = _lineSeparatorTag;
            }

            foreach (var span in spans)
                context.AddTag(new TagSpan<StringIndentationTag>(span.ToSnapshotSpan(snapshotSpan.Snapshot), tag));
        }
    }
}
