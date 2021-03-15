// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Implementation.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.UnderlineReassignment
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
            IForegroundNotificationService notificationService,
            ClassificationTypeMap typeMap,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(threadingContext, listenerProvider.GetListener(FeatureAttribute.Classification), notificationService)
        {
            _typeMap = typeMap;
        }

        protected override ITaggerEventSource CreateEventSource(ITextView textView, ITextBuffer subjectBuffer)
        {
            this.AssertIsForeground();
            const TaggerDelay Delay = TaggerDelay.Short;

            // Note: we don't listen for OnTextChanged.  They'll get reported by the ViewSpan changing and also the
            // SemanticChange notification. 
            // 
            // Note: when the user scrolls, we will try to reclassify as soon as possible.  That way we appear
            // unclassified for a very short amount of time.
            //
            // Note: because we use frozen-partial documents for semantic classification, we may end up with incomplete
            // semantics (esp. during solution load).  Because of this, we also register to hear when the full
            // compilation is available so that reclassify and bring ourselves up to date.
            return new CompilationAvailableTaggerEventSource(
                subjectBuffer, Delay,
                ThreadingContext,
                AsyncListener,
                TaggerEventSources.OnViewSpanChanged(ThreadingContext, textView, textChangeDelay: Delay, scrollChangeDelay: TaggerDelay.NearImmediate),
                TaggerEventSources.OnWorkspaceChanged(subjectBuffer, Delay, this.AsyncListener),
                TaggerEventSources.OnDocumentActiveContextChanged(subjectBuffer, Delay),
                TaggerEventSources.OnOptionChanged(subjectBuffer, );
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

        protected override void ProduceTagsSynchronously(TaggerContext<ClassificationTag> context, DocumentSnapshotSpan spanToTag, int? caretPosition)
        {
            // Assert just so we can find out who is calling this. We should never be used in synchronous scenarios.
            Debug.Fail("Unsupported call to ProduceTagsSynchronously");
        }

        protected override Task ProduceTagsAsync(TaggerContext<ClassificationTag> context, DocumentSnapshotSpan spanToTag, int? caretPosition)
        {
            context.AddTag(new TagSpan<ClassificationTag>(
                new SnapshotSpan(spanToTag.SnapshotSpan.Snapshot, 0, 5),
                new ClassificationTag(_typeMap.GetClassificationType(ClassificationTypeNames.ReassignedVariable))));
            return Task.CompletedTask;
        }
    }
}
