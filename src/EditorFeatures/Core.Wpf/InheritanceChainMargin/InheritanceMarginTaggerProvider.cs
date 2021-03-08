// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
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
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.InheritanceChainMargin
{
    [Export(typeof(IViewTaggerProvider))]
    [Name(nameof(InheritanceMarginTaggerProvider))]
    [TagType(typeof(InheritanceMarginTag))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    internal class InheritanceMarginTaggerProvider : AsynchronousViewTaggerProvider<InheritanceMarginTag>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
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
            var inheritanceMarginInfoService = document.GetRequiredLanguageService<IInheritanceChainService>();
            var memberInfo = await inheritanceMarginInfoService.GetInheritanceInfoForLineAsync(document, cancellationToken).ConfigureAwait(false);
        }
    }
}
