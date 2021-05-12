// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.CodeLens;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeLens.Tagger
{
    internal abstract class AbstractCodeLensViewTaggerProvider : AsynchronousViewTaggerProvider<ICodeLensTag>
    {
        protected AbstractCodeLensViewTaggerProvider(
            IThreadingContext threadingContext,
            IAsynchronousOperationListenerProvider asyncListenerProvider)
            : base(threadingContext, asyncListenerProvider.GetListener(FeatureAttribute.CodeLens))
        {
        }

        protected abstract ImmutableArray<CodeLensNodeInfo> ComputeNodeInfo(
            SyntaxNode root, TextSpan span, CancellationToken cancellationToken);

        protected sealed override TaggerDelay EventChangeDelay => TaggerDelay.OnIdle;

        protected sealed override ITaggerEventSource CreateEventSource(ITextView textView, ITextBuffer subjectBuffer)
        {
            // Note: we don't listen for OnTextChanged.  They'll get reported by the ViewSpan changing and also the
            // SemanticChange notification. 
            // 
            // Note: because we use frozen-partial documents for semantic classification, we may end up with incomplete
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
            var visibleSpanOpt = textView.GetVisibleLinesSpan(subjectBuffer, extraLines: 100);
            if (visibleSpanOpt == null)
            {
                // Couldn't find anything visible, just fall back to tagging everything.
                return base.GetSpansToTag(textView, subjectBuffer);
            }

            return SpecializedCollections.SingletonEnumerable(visibleSpanOpt.Value);
        }

        protected sealed override async Task ProduceTagsAsync(
            TaggerContext<ICodeLensTag> context,
            DocumentSnapshotSpan spanToTag,
            int? caretPosition)
        {
            var cancellationToken = context.CancellationToken;

            var document = spanToTag.Document;
            if (document == null)
                return;

            var workspace = document.Project.Solution.Workspace as VisualStudioWorkspace;
            var guid = workspace?.GetProjectGuid(document.Project.Id) ?? Guid.Empty;

            var snapshotSpan = spanToTag.SnapshotSpan;
            var snapshot = snapshotSpan.Snapshot;
            var span = snapshotSpan.Span;
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            foreach (var info in ComputeNodeInfo(root, span.ToTextSpan(), cancellationToken))
            {
                if (info.Identifier.Span.IsEmpty)
                    continue;

                // HACK: move the codelens tag one line down so it doesn't collide with teh platform tag.
                // Remove once we find a way to disalbe the platform side.
                var start = info.Identifier.SpanStart;
                var lineNumber = snapshot.GetLineNumberFromPosition(start);
                if (lineNumber + 1 < snapshot.LineCount)
                {
                    start = snapshot.GetLineFromLineNumber(lineNumber + 1).Start;
                    var descriptor = new CodeLensDescriptor(guid, document, info);
                    context.AddTag(new TagSpan<ICodeLensTag>(
                        new SnapshotSpan(spanToTag.SnapshotSpan.Snapshot, new Span(start, 0)),
                        new CodeLensTag(descriptor)));
                }
            }
        }

        protected override bool TagEquals(ICodeLensTag previousTag, ICodeLensTag currentTag)
        {
            if (!previousTag.Equals(currentTag))
                return false;

            return true;
        }
    }
}
