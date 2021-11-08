// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.LanguageServer;
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
        private readonly IGlobalOptionService _globalOptionsService;

        // We want to track text changes so that we can try to only reclassify a method body if
        // all edits were contained within one.
        protected override TaggerTextChangeBehavior TextChangeBehavior => TaggerTextChangeBehavior.TrackTextChanges;
        protected override IEnumerable<Option2<bool>> Options => SpecializedCollections.SingletonEnumerable(InternalFeatureOnOffOptions.SemanticColorizer);

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public SemanticClassificationViewTaggerProvider(
            IThreadingContext threadingContext,
            ClassificationTypeMap typeMap,
            IAsynchronousOperationListenerProvider listenerProvider,
            IGlobalOptionService globalOptionsService)
            : base(threadingContext, listenerProvider.GetListener(FeatureAttribute.Classification))
        {
            _typeMap = typeMap;
            _globalOptionsService = globalOptionsService;
        }

        protected override TaggerDelay EventChangeDelay => TaggerDelay.Short;

        protected override ITaggerEventSource CreateEventSource(ITextView textView, ITextBuffer subjectBuffer)
        {
            this.AssertIsForeground();

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
                TaggerEventSources.OnWorkspaceChanged(subjectBuffer, this.AsyncListener),
                TaggerEventSources.OnDocumentActiveContextChanged(subjectBuffer),
                TaggerEventSources.OnOptionChanged(subjectBuffer, ClassificationOptions.Metadata.ClassifyReassignedVariables));
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

        protected override Task ProduceTagsAsync(
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
            var isLspSemanticTokensEnabled = _globalOptionsService.GetOption(LspOptions.LspSemanticTokensFeatureFlag);
            if (isLspSemanticTokensEnabled)
            {
                return Task.CompletedTask;
            }

            return SemanticClassificationUtilities.ProduceTagsAsync(
                context, spanToTag, classificationService, _typeMap, cancellationToken);
        }
    }
}
