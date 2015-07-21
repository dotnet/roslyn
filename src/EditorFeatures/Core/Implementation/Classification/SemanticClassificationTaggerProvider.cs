// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Classification
{
    using Context = AsynchronousTaggerContext<IClassificationTag, object>;

    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IClassificationTag))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    internal partial class SemanticClassificationTaggerProvider :
        ForegroundThreadAffinitizedObject,
        ITaggerProvider,
        IAsynchronousTaggerDataSource<IClassificationTag, object>
    {
        private readonly ISemanticChangeNotificationService _semanticChangeNotificationService;
        private readonly ClassificationTypeMap _typeMap;
        private readonly Lazy<ITaggerProvider> _asynchronousTaggerProvider;

        // We don't want to remove a tag just because it intersected an edit.  This can 
        // cause flashing when a edit touches the edge of a classified symbol without
        // changing it.  For example, if you have "Console." and you remove the <dot>,
        // then you don't want to remove the classification for 'Console'.
        public TaggerDelay? UIUpdateDelay => null;
        public bool IgnoreCaretMovementToExistingTag => false;
        public bool RemoveTagsThatIntersectEdits => false;
        public IEqualityComparer<IClassificationTag> TagComparer => null;
        public SpanTrackingMode SpanTrackingMode => SpanTrackingMode.EdgeExclusive;
        public bool ComputeTagsSynchronouslyIfNoAsynchronousComputationHasCompleted => false;

        public IEnumerable<Option<bool>> Options => SpecializedCollections.SingletonEnumerable(InternalFeatureOnOffOptions.SemanticColorizer);
        public IEnumerable<PerLanguageOption<bool>> PerLanguageOptions => null;

        private IEditorClassificationService _classificationService;

        [ImportingConstructor]
        public SemanticClassificationTaggerProvider(
            IForegroundNotificationService notificationService,
            ISemanticChangeNotificationService semanticChangeNotificationService,
            ClassificationTypeMap typeMap,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            _semanticChangeNotificationService = semanticChangeNotificationService;
            _typeMap = typeMap;
            _asynchronousTaggerProvider = new Lazy<ITaggerProvider>(() =>
                new AsynchronousBufferTaggerProviderWithTagSource<IClassificationTag, object>(
                    this,
                    new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.Classification),
                    notificationService,
                    CreateTagSource));
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return _asynchronousTaggerProvider.Value.CreateTagger<T>(buffer);
        }

        private ProducerPopulatedTagSource<IClassificationTag, object> CreateTagSource(
            ITextView textViewOpt, ITextBuffer subjectBuffer,
            IAsynchronousOperationListener asyncListener, IForegroundNotificationService notificationService)
        {
            return new SemanticClassificationTagSource(subjectBuffer, this, asyncListener, notificationService);
        }

        public ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            return TaggerEventSources.Compose(
                TaggerEventSources.OnSemanticChanged(subjectBuffer, TaggerDelay.Short, _semanticChangeNotificationService),
                TaggerEventSources.OnDocumentActiveContextChanged(subjectBuffer, TaggerDelay.Short),
                TaggerEventSources.OnTextChanged(subjectBuffer, TaggerDelay.Short, reportChangedSpans: true));
        }

        public IEnumerable<SnapshotSpan> GetSpansToTag(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            return null;
        }

        public Task ProduceTagsAsync(Context context)
        {
            return TaggerUtilities.Delegate(context, ProduceTagsAsync);
        }

        private async Task ProduceTagsAsync(Context context, DocumentSnapshotSpan documentSnapshotSpan, int? caretPosition)
        {
            try
            {
                var cancellationToken = context.CancellationToken;
                var document = documentSnapshotSpan.Document;
                var snapshotSpan = documentSnapshotSpan.SnapshotSpan;
                var snapshot = snapshotSpan.Snapshot;
                if (document == null)
                {
                    return;
                }

                if (_classificationService == null)
                {
                    _classificationService = document.Project.LanguageServices.GetService<IEditorClassificationService>();
                }

                if (_classificationService == null)
                {
                    return;
                }

                // we don't directly reference the semantic model here, we just keep it alive so 
                // the classification service does not need to block to produce it.
                using (Logger.LogBlock(FunctionId.Tagger_SemanticClassification_TagProducer_ProduceTags, cancellationToken))
                {
                    var textSpan = snapshotSpan.Span.ToTextSpan();
                    var extensionManager = document.Project.Solution.Workspace.Services.GetService<IExtensionManager>();

                    var classifiedSpans = ClassificationUtilities.GetOrCreateClassifiedSpanList();

                    await _classificationService.AddSemanticClassificationsAsync(
                        document, textSpan, classifiedSpans, cancellationToken: cancellationToken).ConfigureAwait(false);

                    ClassificationUtilities.Convert(_typeMap, snapshotSpan.Snapshot, classifiedSpans, context.AddTag);
                    ClassificationUtilities.ReturnClassifiedSpanList(classifiedSpans);
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }
    }
}
