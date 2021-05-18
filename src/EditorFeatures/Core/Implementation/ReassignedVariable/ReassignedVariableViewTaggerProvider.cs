// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.ReassignedVariable;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.ReassignedVariable
{
    [Export(typeof(IViewTaggerProvider))]
    [TagType(typeof(ClassificationTag))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    internal class ReassignedVariableViewTaggerProvider : AsynchronousViewTaggerProvider<ClassificationTag>
    {
        private readonly ClassificationTypeMap _typeMap;

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public ReassignedVariableViewTaggerProvider(
            IThreadingContext threadingContext,
            ClassificationTypeMap typeMap,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(threadingContext, listenerProvider.GetListener(FeatureAttribute.Classification))
        {
            _typeMap = typeMap;
        }

        protected override TaggerDelay EventChangeDelay => TaggerDelay.Short;

        /// <summary>
        /// Rerun the tagger whenever our option changes.
        /// </summary>
        protected override IEnumerable<PerLanguageOption2<bool>> PerLanguageOptions
            => SpecializedCollections.SingletonEnumerable(ReassignedVariableOptions.Underline);

        protected override ITaggerEventSource CreateEventSource(ITextView textView, ITextBuffer subjectBuffer)
        {
            this.AssertIsForeground();

            // Note: we don't listen for OnTextChanged.  They'll get reported by the ViewSpan changing and also the
            // SemanticChange notification. 
            // 
            // Note: when the user scrolls, we will try to reclassify variables as soon as possible.  That way we appear
            // unclassified for a very short amount of time.
            //
            // Note: because we use frozen-partial documents for classifying these, we may end up with incomplete
            // semantics (esp. during solution load).  Because of this, we also register to hear when the full
            // compilation is available so that reclassify and bring ourselves up to date.
            return new CompilationAvailableTaggerEventSource(
                subjectBuffer,
                AsyncListener,
                TaggerEventSources.OnViewSpanChanged(ThreadingContext, textView),
                TaggerEventSources.OnWorkspaceChanged(subjectBuffer, this.AsyncListener),
                TaggerEventSources.OnDocumentActiveContextChanged(subjectBuffer));
        }

        protected override IEnumerable<SnapshotSpan> GetSpansToTag(ITextView textView, ITextBuffer subjectBuffer)
        {
            this.AssertIsForeground();

            // Find the visible span some 100 lines +/- what's actually in view.  This way
            // if the user scrolls up/down, we'll already have the results.
            var visibleSpan = textView.GetVisibleLinesSpan(subjectBuffer, extraLines: 100);
            if (visibleSpan == null)
            {
                // Couldn't find anything visible, just fall back to classifying everything.
                return base.GetSpansToTag(textView, subjectBuffer);
            }

            return SpecializedCollections.SingletonEnumerable(visibleSpan.Value);
        }

        protected override async Task ProduceTagsAsync(TaggerContext<ClassificationTag> context, DocumentSnapshotSpan spanToTag, int? caretPosition)
        {
            var cancellationToken = context.CancellationToken;

            var document = spanToTag.Document;
            if (document == null)
                return;

            var option = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var enabled = option.GetOption(ReassignedVariableOptions.Underline);
            if (!enabled)
                return;

            // Don't force the entire compilation to be ready, just to determine if something is assigned or not.  It's
            // expensive, and not actually needed to answer the question accurately enough.
            document = document.WithFrozenPartialSemantics(cancellationToken);
            var service = document.GetLanguageService<IReassignedVariableService>();
            if (service == null)
                return;

            var snapshotSpan = spanToTag.SnapshotSpan;
            var reassignedVariables = await service.GetReassignedVariablesAsync(
                document, snapshotSpan.Span.ToTextSpan(), cancellationToken).ConfigureAwait(false);

            var tag = new ClassificationTag(_typeMap.GetClassificationType(
                ReassignedVariableClassificationTypeDefinitions.ReassignedVariable));
            foreach (var variable in reassignedVariables)
                context.AddTag(new TagSpan<ClassificationTag>(variable.ToSnapshotSpan(snapshotSpan.Snapshot), tag));
        }
    }
}
