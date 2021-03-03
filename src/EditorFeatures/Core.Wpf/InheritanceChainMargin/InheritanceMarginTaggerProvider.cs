// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InheritanceChainMargin;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.InheritanceChainMargin
{
    [Export(typeof(IViewTaggerProvider))]
    [Shared]
    internal class InheritanceMarginTaggerProvider : AsynchronousViewTaggerProvider<InheritanceMarginTag>
    {
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        [ImportingConstructor]
        public InheritanceMarginTaggerProvider(
            IThreadingContext threadingContext,
            IAsynchronousOperationListenerProvider listenerProvider,
            IForegroundNotificationService notificationService) : base(
                threadingContext,
                listenerProvider.GetListener(FeatureAttribute.InheritanceChainMargin),
                notificationService)
        {
        }

        protected override ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
            => TaggerEventSources.Compose(TaggerEventSources.OnWorkspaceChanged(subjectBuffer, TaggerDelay.OnIdle, AsyncListener));

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
            var inheritanceMarginInfoService = document.GetRequiredLanguageService<IInheritanceChainInfoService>();
            var inheritanceInfoForDocument = await inheritanceMarginInfoService.GetInheritanceInfoForLineAsync(document, cancellationToken).ConfigureAwait(false);

            foreach (var info in inheritanceInfoForDocument)
            {
                context.AddTag(new TagSpan<InheritanceMarginTag>(
                    spanToTag.SnapshotSpan,
                    InheritanceMarginTag.FromInheritanceInfo(info)));
            }
        }
    }
}
