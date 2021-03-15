// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
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
    [Name(nameof(InheritanceChainMarginTaggerProvider))]
    [TagType(typeof(InheritanceMarginTag))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    internal class InheritanceChainMarginTaggerProvider : AsynchronousViewTaggerProvider<InheritanceMarginTag>
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
        {
            return TaggerEventSources.Compose(
                TaggerEventSources.OnWorkspaceChanged(subjectBuffer, TaggerDelay.OnIdle, AsyncListener),
                TaggerEventSources.OnTextChanged(subjectBuffer, TaggerDelay.OnIdle));
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
            var inheritanceMarginInfoService = document.GetLanguageService<IInheritanceMarginService>();
            if (inheritanceMarginInfoService == null)
            {
                return;
            }

            var inheritanceMemberItems = await inheritanceMarginInfoService
                .GetInheritanceInfoForLineAsync(
                    document,
                    cancellationToken).ConfigureAwait(false);

            if (inheritanceMemberItems.IsEmpty)
            {
                return;
            }

            // One line might have multiple members to show, so group them.
            // For example:
            // interface IBar { void Foo1(); void Foo2() }
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
                    var taggedSpan = new SnapshotSpan(snapshot, line.Start, length: line.Length);
                    context.AddTag(new TagSpan<InheritanceMarginTag>(
                        taggedSpan,
                        InheritanceMarginTag.FromInheritanceInfo(membersOnTheLine)));
                }
            }
        }
    }
}
