// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.InheritanceMargin
{
    [Export(typeof(IViewTaggerProvider))]
    [TagType(typeof(InheritanceMarginTag))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(nameof(InheritanceChainMarginTaggerProvider))]
    internal sealed class InheritanceChainMarginTaggerProvider : AsynchronousViewTaggerProvider<InheritanceMarginTag>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InheritanceChainMarginTaggerProvider(
            IThreadingContext threadingContext,
            IAsynchronousOperationListenerProvider listenerProvider,
            IForegroundNotificationService notificationService) : base(
                threadingContext,
                listenerProvider.GetListener(FeatureAttribute.InheritanceChainMargin),
                notificationService)
        {
        }

        protected override ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
            => new CompilationAvailableTaggerEventSource(
                subjectBuffer,
                TaggerDelay.OnIdle,
                ThreadingContext,
                AsyncListener,
                TaggerEventSources.OnWorkspaceChanged(subjectBuffer, TaggerDelay.OnIdle, AsyncListener),
                TaggerEventSources.OnViewSpanChanged(ThreadingContext, textViewOpt, TaggerDelay.OnIdle, TaggerDelay.OnIdle),
                TaggerEventSources.OnDocumentActiveContextChanged(subjectBuffer, TaggerDelay.OnIdle),
                TaggerEventSources.OnOptionChanged(subjectBuffer, FeatureOnOffOptions.ShowInheritanceMargin, TaggerDelay.OnIdle));

        protected override IEnumerable<SnapshotSpan> GetSpansToTag(ITextView textView, ITextBuffer subjectBuffer)
        {
            this.AssertIsForeground();

            var visibleSpan = textView.GetVisibleLinesSpan(subjectBuffer, extraLines: 100);
            if (visibleSpan == null)
            {
                return base.GetSpansToTag(textView, subjectBuffer);
            }

            return SpecializedCollections.SingletonEnumerable(visibleSpan.Value);
        }

        protected override async Task ProduceTagsAsync(
            TaggerContext<InheritanceMarginTag> context,
            DocumentSnapshotSpan spanToTag,
            int? caretPosition)
        {
            var document = spanToTag.Document;
            if (document == null)
            {
                return;
            }

            var cancellationToken = context.CancellationToken;

            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var featureEnabled = options.GetOption(FeatureOnOffOptions.ShowInheritanceMargin);
            if (!featureEnabled)
            {
                return;
            }

            var inheritanceMarginInfoService = document.WithFrozenPartialSemantics(cancellationToken).GetLanguageService<IInheritanceMarginService>();
            if (inheritanceMarginInfoService == null)
            {
                return;
            }

            var inheritanceMemberItems = await inheritanceMarginInfoService.GetInheritanceInfoAsync(
                    document,
                    spanToTag.SnapshotSpan.Span.ToTextSpan(),
                    cancellationToken).ConfigureAwait(false);

            if (inheritanceMemberItems.IsEmpty)
            {
                return;
            }

            // One line might have multiple members to show, so group them.
            // For example:
            // interface IBar { void Foo1(); void Foo2(); }
            // class Bar : IBar { void Foo1() { } void Foo2() { } }
            var lineToMembers = inheritanceMemberItems
                .GroupBy(item => item.LineNumber)
                .ToImmutableDictionary(
                    keySelector: grouping => grouping.Key,
                    elementSelector: grouping => grouping.SelectAsArray(g => g));

            var snapshot = spanToTag.SnapshotSpan.Snapshot;
            foreach (var (lineNumber, membersOnTheLine) in lineToMembers)
            {
                if (membersOnTheLine.Length > 0)
                {
                    var line = snapshot.GetLineFromLineNumber(lineNumber);
                    // We only care about the line, so just tag one char.
                    var taggedSpan = new SnapshotSpan(snapshot, line.Start, length: 1);
                    context.AddTag(new TagSpan<InheritanceMarginTag>(
                        taggedSpan,
                        new InheritanceMarginTag(membersOnTheLine)));
                }
            }
        }
    }
}
