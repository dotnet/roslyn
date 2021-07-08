﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
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
    internal sealed class InheritanceMarginTaggerProvider : AsynchronousViewTaggerProvider<InheritanceMarginTag>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InheritanceMarginTaggerProvider(
            IThreadingContext threadingContext,
            IAsynchronousOperationListenerProvider listenerProvider) : base(
                threadingContext,
                listenerProvider.GetListener(FeatureAttribute.InheritanceMargin))
        {
        }

        protected override TaggerDelay EventChangeDelay => TaggerDelay.OnIdle;

        private bool? _experimentEnabled = null;

        protected override ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
            // Because we use frozen-partial documents for semantic classification, we may end up with incomplete
            // semantics (esp. during solution load).  Because of this, we also register to hear when the full
            // compilation is available so that reclassify and bring ourselves up to date.
            => new CompilationAvailableTaggerEventSource(
                subjectBuffer,
                AsyncListener,
                TaggerEventSources.OnWorkspaceChanged(subjectBuffer, AsyncListener),
                TaggerEventSources.OnViewSpanChanged(ThreadingContext, textViewOpt),
                TaggerEventSources.OnDocumentActiveContextChanged(subjectBuffer),
                TaggerEventSources.OnOptionChanged(subjectBuffer, FeatureOnOffOptions.ShowInheritanceMargin));

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

            var optionIsChecked = options.GetOption(FeatureOnOffOptions.ShowInheritanceMargin);
            if (_experimentEnabled is null)
            {
                var experimentationService = document.Project.Solution.Workspace.Services.GetRequiredService<IExperimentationService>();
                _experimentEnabled = experimentationService.IsExperimentEnabled(WellKnownExperimentNames.InheritanceMargin);
            }

            var shouldEnableFeature = optionIsChecked == true || (_experimentEnabled == true && optionIsChecked == null);
            if (!shouldEnableFeature)
            {
                return;
            }

            // Use FrozenSemantics Version of document to get the semantics ready, therefore we could have faster
            // response. (Since the full load might take a long time)
            // We also subscribe to CompilationAvailableTaggerEventSource, so this will finally reach the correct state.
            var inheritanceMarginInfoService = document.WithFrozenPartialSemantics(cancellationToken).GetLanguageService<IInheritanceMarginService>();
            if (inheritanceMarginInfoService == null)
            {
                return;
            }

            var inheritanceMemberItems = ImmutableArray<InheritanceMarginItem>.Empty;
            using (Logger.LogBlock(FunctionId.InheritanceMargin_GetInheritanceMemberItems, cancellationToken, LogLevel.Information))
            {
                inheritanceMemberItems = await inheritanceMarginInfoService.GetInheritanceMemberItemsAsync(
                    document,
                    spanToTag.SnapshotSpan.Span.ToTextSpan(),
                    cancellationToken).ConfigureAwait(false);
            }

            if (inheritanceMemberItems.IsEmpty)
            {
                return;
            }

            // One line might have multiple members to show, so group them.
            // For example:
            // interface IBar { void Foo1(); void Foo2(); }
            // class Bar : IBar { void Foo1() { } void Foo2() { } }
            var lineToMembers = inheritanceMemberItems
                .GroupBy(item => item.LineNumber);

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
                    new InheritanceMarginTag(document.Project.Solution.Workspace, lineNumber, membersOnTheLineArray)));
            }
        }
    }
}
