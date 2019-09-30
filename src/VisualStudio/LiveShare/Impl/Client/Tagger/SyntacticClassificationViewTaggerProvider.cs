// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Classification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using TPL = System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Tagger
{
    /// <summary>
    /// this is almost straight copy from typescript for syntatic LSP experiement.
    /// we won't attempt to change code to follow Roslyn style until we have result of the experiement
    /// </summary>
    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class SyntacticClassificationViewTaggerProvider : AbstractAsyncClassificationTaggerProvider
    {
        private readonly ClassificationTypeMap _typeMap;

        [ImportingConstructor]
        public SyntacticClassificationViewTaggerProvider(
            IForegroundNotificationService notificationService,
            ClassificationTypeMap typeMap,
            IAsynchronousOperationListenerProvider asyncListenerProvider,
            IThreadingContext threadingContext)
            : base(notificationService, asyncListenerProvider, threadingContext)
        {
            _typeMap = typeMap;
        }

        protected override SpanTrackingMode SpanTrackingMode => SpanTrackingMode.EdgeExclusive;

        protected override ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            return TaggerEventSources.OnTextChanged(subjectBuffer, TaggerDelay.NearImmediate);
        }

        protected override async TPL.Task ProduceTagsAsync(
            TaggerContext<IClassificationTag> taggerContext,
            DocumentSnapshotSpan snapshotSpan)
        {
            // We should check this at the call site.
            // This a safety check to make sure we do this when 
            // we introduce a new call site.
            Debug.Assert(snapshotSpan.Document != null);

            var document = snapshotSpan.Document;
            var cancellationToken = taggerContext.CancellationToken;

            var classificationService = document.Project.LanguageServices.GetService<IClassificationService>() as IRemoteClassificationService;
            if (classificationService == null)
            {
                return;
            }

            var classifiedSpans = ClassificationUtilities.GetOrCreateClassifiedSpanList();
            var tagSpan = TextSpan.FromBounds(snapshotSpan.SnapshotSpan.Start, snapshotSpan.SnapshotSpan.End);
            await classificationService.AddRemoteSyntacticClassificationsAsync(document, tagSpan, classifiedSpans, cancellationToken).ConfigureAwait(false);

            ClassificationUtilities.Convert(_typeMap, snapshotSpan.SnapshotSpan.Snapshot, classifiedSpans, taggerContext.AddTag);
            ClassificationUtilities.ReturnClassifiedSpanList(classifiedSpans);
        }
    }
}
