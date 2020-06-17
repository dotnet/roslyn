
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.InlineParamNameHints
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("csharp")]
    [TagType(typeof(IntraTextAdornmentTag))]
    [Name("InlineParamNameHintsTaggerProvider")]
    internal class InlineParamNameHintsTaggerProvider : AsynchronousTaggerProvider<InlineParamHintsTag>
    {
        private TextFormattingRunProperties _format;

        [ImportingConstructor]
        public InlineParamNameHintsTaggerProvider(
            IThreadingContext threadingContext,
            IAsynchronousOperationListenerProvider listenerProvider,
            IForegroundNotificationService notificationService)
            : base(threadingContext, listenerProvider.GetListener(FeatureAttribute.InlineParamNameHints), notificationService)
        {
        }

        protected override ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            return TaggerEventSources.OnTextChanged(subjectBuffer, TaggerDelay.NearImmediate);
        }

        protected override async Task ProduceTagsAsync(TaggerContext<InlineParamHintsTag> context, DocumentSnapshotSpan documentSnapshotSpan, int? caretPosition)
        {
            var cancellationToken = context.CancellationToken;
            var document = documentSnapshotSpan.Document;

            var snapshotSpan = documentSnapshotSpan.SnapshotSpan;
            var paramNameHintsService = document.GetLanguageService<InlineParameterNameHintsService.IInlineParamNameHintsService>();
            var paramNameHintSpans = await paramNameHintsService.GetInlineParameterNameHintsAsync(document, snapshotSpan.Span.ToTextSpan(), cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var span in paramNameHintSpans)
            { 
                context.AddTag(new TagSpan<InlineParamHintsTag>(span.Item2.ToSnapshotSpan(snapshotSpan.Snapshot), new InlineParamHintsTag(span.Item1, span.Item1.Length + 1, _format)));
            }
        }
    }
}
