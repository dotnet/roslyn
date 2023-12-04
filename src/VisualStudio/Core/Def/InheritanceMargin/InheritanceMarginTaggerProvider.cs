// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Workspaces;
using Microsoft.VisualStudio.LanguageServices.InheritanceMargin;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin
{
    [Export(typeof(IViewTaggerProvider))]
    [TagType(typeof(InheritanceMarginTag))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(nameof(InheritanceMarginTaggerProvider))]
    internal sealed class InheritanceMarginTaggerProvider : AsynchronousViewportTaggerProvider<InheritanceMarginTag>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InheritanceMarginTaggerProvider(
            IThreadingContext threadingContext,
            IGlobalOptionService globalOptions,
            [Import(AllowDefault = true)] ITextBufferVisibilityTracker? visibilityTracker,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(
                threadingContext,
                globalOptions,
                visibilityTracker,
                listenerProvider.GetListener(FeatureAttribute.InheritanceMargin))
        {
        }

        protected override TaggerDelay EventChangeDelay => TaggerDelay.OnIdle;

        protected override ITaggerEventSource CreateEventSource(ITextView textView, ITextBuffer subjectBuffer)
        {
            // Because we use frozen-partial documents for semantic classification, we may end up with incomplete
            // semantics (esp. during solution load).  Because of this, we also register to hear when the full
            // compilation is available so that reclassify and bring ourselves up to date.
            // Note: Also generate tags when InheritanceMarginOptions.InheritanceMarginCombinedWithIndicatorMargin is changed,
            // because we want to refresh the glyphs in indicator margin.
            return new CompilationAvailableTaggerEventSource(
               subjectBuffer,
               AsyncListener,
               TaggerEventSources.OnWorkspaceChanged(subjectBuffer, AsyncListener),
               TaggerEventSources.OnViewSpanChanged(ThreadingContext, textView),
               TaggerEventSources.OnDocumentActiveContextChanged(subjectBuffer),
               TaggerEventSources.OnGlobalOptionChanged(GlobalOptions, InheritanceMarginOptionsStorage.ShowInheritanceMargin),
               TaggerEventSources.OnGlobalOptionChanged(GlobalOptions, InheritanceMarginOptionsStorage.InheritanceMarginCombinedWithIndicatorMargin));
        }

        protected override async Task ProduceTagsAsync(
            TaggerContext<InheritanceMarginTag> context,
            DocumentSnapshotSpan spanToTag,
            CancellationToken cancellationToken)
        {
            var document = spanToTag.Document;
            if (document == null)
                return;

            if (document.Project.Solution.WorkspaceKind == WorkspaceKind.Interactive)
                return;

            var inheritanceMarginInfoService = document.GetLanguageService<IInheritanceMarginService>();
            if (inheritanceMarginInfoService == null)
                return;

            if (GlobalOptions.GetOption(InheritanceMarginOptionsStorage.ShowInheritanceMargin, document.Project.Language) == false)
                return;

            var includeGlobalImports = GlobalOptions.GetOption(InheritanceMarginOptionsStorage.InheritanceMarginIncludeGlobalImports, document.Project.Language);

            // Use FrozenSemantics Version of document to get the semantics ready, therefore we could have faster
            // response. (Since the full load might take a long time)
            // We also subscribe to CompilationAvailableTaggerEventSource, so this will finally reach the correct state.
            document = document.WithFrozenPartialSemantics(cancellationToken);

            var spanToSearch = spanToTag.SnapshotSpan.Span.ToTextSpan();
            var stopwatch = SharedStopwatch.StartNew();
            var inheritanceMemberItems = await inheritanceMarginInfoService.GetInheritanceMemberItemsAsync(
                document,
                spanToSearch,
                includeGlobalImports,
                frozenPartialSemantics: true,
                cancellationToken).ConfigureAwait(false);
            var elapsed = stopwatch.Elapsed;

            if (inheritanceMemberItems.IsEmpty)
                return;

            InheritanceMarginLogger.LogGenerateBackgroundInheritanceInfo(elapsed);

            // One line might have multiple members to show, so group them.
            // For example:
            // interface IBar { void Foo1(); void Foo2(); }
            // class Bar : IBar { void Foo1() { } void Foo2() { } }
            var lineToMembers = inheritanceMemberItems.GroupBy(item => item.LineNumber);

            var snapshot = spanToTag.SnapshotSpan.Snapshot;

            foreach (var (lineNumber, membersOnTheLine) in lineToMembers)
            {
                var membersOnTheLineArray = membersOnTheLine.ToImmutableArray();

                // One line should at least have one member on it.
                Contract.ThrowIfTrue(membersOnTheLineArray.IsEmpty);

                var line = snapshot.GetLineFromLineNumber(lineNumber);
                // We only care about the line, so just tag the start.
                context.AddTag(new TagSpan<InheritanceMarginTag>(
                    new SnapshotSpan(snapshot, line.Start, length: 0),
                    new InheritanceMarginTag(lineNumber, membersOnTheLineArray)));
            }
        }

        protected override bool TagEquals(InheritanceMarginTag tag1, InheritanceMarginTag tag2)
            => tag1.Equals(tag2);
    }
}
