// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.InlineHints
{
    /// <summary>
    /// The TaggerProvider that calls upon the service in order to locate the spans and names
    /// </summary>
    [Export(typeof(IViewTaggerProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TagType(typeof(InlineHintDataTag))]
    [Name(nameof(InlineHintsDataTaggerProvider))]
    internal class InlineHintsDataTaggerProvider : AsynchronousViewTaggerProvider<InlineHintDataTag>
    {
        private static readonly SymbolDisplayFormat s_minimalTypeStyle = new SymbolDisplayFormat(
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.AllowDefaultLiteral | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        private readonly IAsynchronousOperationListener _listener;

        protected override SpanTrackingMode SpanTrackingMode => SpanTrackingMode.EdgeInclusive;

        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        [ImportingConstructor]
        public InlineHintsDataTaggerProvider(
            IThreadingContext threadingContext,
            IAsynchronousOperationListenerProvider listenerProvider,
            IForegroundNotificationService notificationService)
            : base(threadingContext, listenerProvider.GetListener(FeatureAttribute.InlineParameterNameHints), notificationService)
        {
            _listener = listenerProvider.GetListener(FeatureAttribute.InlineParameterNameHints);
        }

        protected override ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            return TaggerEventSources.Compose(
                TaggerEventSources.OnViewSpanChanged(ThreadingContext, textViewOpt, textChangeDelay: TaggerDelay.Short, scrollChangeDelay: TaggerDelay.NearImmediate),
                TaggerEventSources.OnWorkspaceChanged(subjectBuffer, TaggerDelay.NearImmediate, _listener),
                TaggerEventSources.OnOptionChanged(subjectBuffer, InlineHintsOptions.DisplayAllOverride, TaggerDelay.NearImmediate),
                TaggerEventSources.OnOptionChanged(subjectBuffer, InlineHintsOptions.EnabledForParameters, TaggerDelay.NearImmediate),
                TaggerEventSources.OnOptionChanged(subjectBuffer, InlineHintsOptions.ForLiteralParameters, TaggerDelay.NearImmediate),
                TaggerEventSources.OnOptionChanged(subjectBuffer, InlineHintsOptions.ForObjectCreationParameters, TaggerDelay.NearImmediate),
                TaggerEventSources.OnOptionChanged(subjectBuffer, InlineHintsOptions.ForOtherParameters, TaggerDelay.NearImmediate),
                TaggerEventSources.OnOptionChanged(subjectBuffer, InlineHintsOptions.SuppressForParametersThatMatchMethodIntent, TaggerDelay.NearImmediate),
                TaggerEventSources.OnOptionChanged(subjectBuffer, InlineHintsOptions.SuppressForParametersThatDifferOnlyBySuffix, TaggerDelay.NearImmediate));
        }

        protected override IEnumerable<SnapshotSpan> GetSpansToTag(ITextView textView, ITextBuffer subjectBuffer)
        {
            this.AssertIsForeground();

            // Find the visible span some 100 lines +/- what's actually in view.  This way
            // if the user scrolls up/down, we'll already have the results.
            var visibleSpanOpt = textView.GetVisibleLinesSpan(subjectBuffer, extraLines: 100);
            if (visibleSpanOpt == null)
            {
                // Couldn't find anything visible, just fall back to tagging all hint locations
                return base.GetSpansToTag(textView, subjectBuffer);
            }

            return SpecializedCollections.SingletonEnumerable(visibleSpanOpt.Value);
        }

        protected override async Task ProduceTagsAsync(TaggerContext<InlineHintDataTag> context, DocumentSnapshotSpan documentSnapshotSpan, int? caretPosition)
        {
            var cancellationToken = context.CancellationToken;
            await AddTypeHintsAsync(context, documentSnapshotSpan, cancellationToken).ConfigureAwait(false);
            await AddParameterNameHintsAsync(context, documentSnapshotSpan, cancellationToken).ConfigureAwait(false);
        }

        private async Task AddTypeHintsAsync(TaggerContext<InlineHintDataTag> context, DocumentSnapshotSpan documentSnapshotSpan, CancellationToken cancellationToken)
        {
            var document = documentSnapshotSpan.Document;
            var service = document.GetLanguageService<IInlineTypeHintsService>();
            if (service == null)
                return;

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var anonymousTypeService = document.GetRequiredLanguageService<IAnonymousTypeDisplayService>();

            var snapshotSpan = documentSnapshotSpan.SnapshotSpan;
            var position = snapshotSpan.Span.Start;
            var hints = await service.GetInlineTypeHintsAsync(document, snapshotSpan.Span.ToTextSpan(), cancellationToken).ConfigureAwait(false);
            foreach (var hint in hints)
            {
                Contract.ThrowIfNull(hint.Type);

                var sb = PooledStringBuilder.GetInstance();
                var parts = hint.Type.ToDisplayParts(s_minimalTypeStyle);

                AddParts(anonymousTypeService, sb, parts, semanticModel, position);

                cancellationToken.ThrowIfCancellationRequested();
                context.AddTag(new TagSpan<InlineHintDataTag>(
                    new SnapshotSpan(snapshotSpan.Snapshot, hint.Position, 0),
                    new InlineHintDataTag(
                        sb.ToStringAndFree(),
                        hint.Type.GetSymbolKey(cancellationToken))));
            }
        }

        private void AddParts(
            IAnonymousTypeDisplayService anonymousTypeService,
            PooledStringBuilder sb,
            System.Collections.Immutable.ImmutableArray<SymbolDisplayPart> parts,
            SemanticModel semanticModel,
            int position,
            HashSet<INamedTypeSymbol>? seenSymbols = null)
        {
            seenSymbols ??= new();

            foreach (var part in parts)
            {
                if (part.Symbol is INamedTypeSymbol { IsAnonymousType: true } anonymousType)
                {
                    if (seenSymbols.Add(anonymousType))
                    {
                        var anonymousParts = anonymousTypeService.GetAnonymousTypeParts(anonymousType, semanticModel, position);
                        AddParts(anonymousTypeService, sb, anonymousParts, semanticModel, position, seenSymbols);
                        seenSymbols.Remove(anonymousType);
                    }
                    else
                    {
                        sb.Builder.Append("...");
                    }
                }
                else
                {
                    sb.Builder.Append(part.ToString());
                }
            }
        }

        private static async Task AddParameterNameHintsAsync(TaggerContext<InlineHintDataTag> context, DocumentSnapshotSpan documentSnapshotSpan, CancellationToken cancellationToken)
        {
            var document = documentSnapshotSpan.Document;
            var service = document.GetLanguageService<IInlineParameterNameHintsService>();
            if (service == null)
                return;

            var snapshotSpan = documentSnapshotSpan.SnapshotSpan;
            var hints = await service.GetInlineParameterNameHintsAsync(document, snapshotSpan.Span.ToTextSpan(), cancellationToken).ConfigureAwait(false);
            foreach (var hint in hints)
            {
                Contract.ThrowIfNull(hint.Parameter);

                cancellationToken.ThrowIfCancellationRequested();
                context.AddTag(new TagSpan<InlineHintDataTag>(
                    new SnapshotSpan(snapshotSpan.Snapshot, hint.Position, 0),
                    new InlineHintDataTag(
                        hint.Parameter.Name + ":",
                        hint.Parameter.GetSymbolKey(cancellationToken))));
            }
        }
    }
}
