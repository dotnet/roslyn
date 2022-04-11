// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Workspaces;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Classification
{
    /// <summary>
    /// This is the tagger we use for view classification scenarios.  It is used for classifying code
    /// in the editor.  We use a view tagger so that we can only classify what's in view, and not
    /// the whole file.
    /// </summary>
    internal abstract class AbstractSemanticOrEmbeddedClassificationViewTaggerProvider : AsynchronousViewTaggerProvider<IClassificationTag>
    {
        private readonly ClassificationTypeMap _typeMap;
        private readonly IGlobalOptionService _globalOptions;
        private readonly ClassificationType _type;

        // We want to track text changes so that we can try to only reclassify a method body if
        // all edits were contained within one.
        protected sealed override TaggerTextChangeBehavior TextChangeBehavior => TaggerTextChangeBehavior.TrackTextChanges;
        protected sealed override IEnumerable<Option2<bool>> Options => SpecializedCollections.SingletonEnumerable(InternalFeatureOnOffOptions.SemanticColorizer);

        protected AbstractSemanticOrEmbeddedClassificationViewTaggerProvider(
            IThreadingContext threadingContext,
            ClassificationTypeMap typeMap,
            IGlobalOptionService globalOptions,
            ITextBufferVisibilityTracker? visibilityTracker,
            IAsynchronousOperationListenerProvider listenerProvider,
            ClassificationType type)
            : base(threadingContext, globalOptions, visibilityTracker, listenerProvider.GetListener(FeatureAttribute.Classification))
        {
            _typeMap = typeMap;
            _globalOptions = globalOptions;
            _type = type;
        }

        protected sealed override TaggerDelay EventChangeDelay => TaggerDelay.Short;

        protected sealed override ITaggerEventSource CreateEventSource(ITextView? textView, ITextBuffer subjectBuffer)
        {
            this.ThreadingContext.ThrowIfNotOnUIThread();
            Contract.ThrowIfNull(textView);

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
                TaggerEventSources.OnWorkspaceChanged(subjectBuffer, AsyncListener),
                TaggerEventSources.OnDocumentActiveContextChanged(subjectBuffer),
                TaggerEventSources.OnOptionChanged(subjectBuffer, ClassificationOptionsStorage.ClassifyReassignedVariables));
        }

        protected sealed override IEnumerable<SnapshotSpan> GetSpansToTag(ITextView? textView, ITextBuffer subjectBuffer)
        {
            this.ThreadingContext.ThrowIfNotOnUIThread();
            Contract.ThrowIfNull(textView);

            // Find the visible span some 100 lines +/- what's actually in view.  This way
            // if the user scrolls up/down, we'll already have the results.
            var visibleSpanOpt = textView.GetVisibleLinesSpan(subjectBuffer, extraLines: 100);
            if (visibleSpanOpt == null)
                // Couldn't find anything visible, just fall back to classifying everything.
                return base.GetSpansToTag(textView, subjectBuffer);

            return SpecializedCollections.SingletonEnumerable(visibleSpanOpt.Value);
        }

        protected sealed override Task ProduceTagsAsync(
            TaggerContext<IClassificationTag> context, CancellationToken cancellationToken)
        {
            Debug.Assert(context.SpansToTag.IsSingle());

            var spanToTag = context.SpansToTag.Single();

            var document = spanToTag.Document;
            if (document == null)
                return Task.CompletedTask;

            // Attempt to get a classification service which will actually produce the results.
            // If we can't (because we have no Document, or because the language doesn't support
            // this service), then bail out immediately.
            var classificationService = document.GetLanguageService<IClassificationService>();
            if (classificationService == null)
                return Task.CompletedTask;

            // The LSP client will handle producing tags when running under the LSP editor.
            // Our tagger implementation should return nothing to prevent conflicts.
            var workspaceContextService = document.Project.Solution.Workspace.Services.GetRequiredService<IWorkspaceContextService>();
            if (workspaceContextService?.IsInLspEditorContext() == true)
                return Task.CompletedTask;

            // If the LSP semantic tokens feature flag is enabled, return nothing to prevent conflicts.
            var isLspSemanticTokensEnabled = _globalOptions.GetOption(LspOptions.LspSemanticTokensFeatureFlag);
            if (isLspSemanticTokensEnabled)
                return Task.CompletedTask;

            var classificationOptions = _globalOptions.GetClassificationOptions(document.Project.Language);
            return ClassificationUtilities.ProduceTagsAsync(
                context, spanToTag, classificationService, _typeMap, classificationOptions, _type, cancellationToken);
        }
    }
}
