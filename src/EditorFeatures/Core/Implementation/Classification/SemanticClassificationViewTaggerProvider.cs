// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Classification
{
    /// <summary>
    /// This is the tagger we use for view classification scenarios.  It is used for classifying code
    /// in the editor.  We use a view tagger so that we can only classify what's in view, and not
    /// the whole file.
    /// </summary>
    [Export(typeof(IViewTaggerProvider))]
    [TagType(typeof(IClassificationTag))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    internal partial class SemanticClassificationViewTaggerProvider : AsynchronousViewTaggerProvider<IClassificationTag>
    {
        private readonly ClassificationTypeMap _typeMap;

        // We want to track text changes so that we can try to only reclassify a method body if
        // all edits were contained within one.
        protected override TaggerTextChangeBehavior TextChangeBehavior => TaggerTextChangeBehavior.TrackTextChanges;
        protected override IEnumerable<Option2<bool>> Options => SpecializedCollections.SingletonEnumerable(InternalFeatureOnOffOptions.SemanticColorizer);

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public SemanticClassificationViewTaggerProvider(
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

            // Note: we don't listen for OnTextChanged.  They'll get reported by the ViewSpan changing 
            // and also the SemanticChange nodification. 
            // 
            // Note: when the user scrolls, we will try to reclassify as soon as possible.  That way
            // we appear semantically unclassified for a very short amount of time.
            return TaggerEventSources.Compose(
                TaggerEventSources.OnViewSpanChanged(ThreadingContext, textView, textChangeDelay: Delay, scrollChangeDelay: TaggerDelay.NearImmediate),
                TaggerEventSources.OnWorkspaceChanged(subjectBuffer, Delay, this.AsyncListener),
                TaggerEventSources.OnDocumentActiveContextChanged(subjectBuffer, Delay));
        }

        protected override IEnumerable<SnapshotSpan> GetSpansToTag(ITextView textView, ITextBuffer subjectBuffer)
        {
            this.AssertIsForeground();

            // Find the visible span some 100 lines +/- what's actually in view.  This way
            // if the user scrolls up/down, we'll already have the results.
            var visibleSpanOpt = textView.GetVisibleLinesSpan(subjectBuffer, extraLines: 100);
            if (visibleSpanOpt == null)
            {
                // Couldn't find anything visible, just fall back to classifying everything.
                return base.GetSpansToTag(textView, subjectBuffer);
            }

            return SpecializedCollections.SingletonEnumerable(visibleSpanOpt.Value);
        }

        protected override Task ProduceTagsAsync(TaggerContext<IClassificationTag> context)
        {
            Debug.Assert(context.SpansToTag.IsSingle());

            var spanToTag = context.SpansToTag.Single();

            var document = spanToTag.Document;

            // Attempt to get a classification service which will actually produce the results.
            // If we can't (because we have no Document, or because the language doesn't support
            // this service), then bail out immediately.
            var classificationService = document?.GetLanguageService<IClassificationService>();
            if (classificationService == null)
            {
                return Task.CompletedTask;
            }

            return SemanticClassificationUtilities.ProduceTagsAsync(context, spanToTag, classificationService, _typeMap);
        }
    }
}
