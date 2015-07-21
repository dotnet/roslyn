// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Classification
{
    using Context = AsynchronousTaggerContext<IClassificationTag, VersionStamp?>;

    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IClassificationTag))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    internal partial class SemanticClassificationTaggerProvider : AsynchronousTaggerProvider<IClassificationTag, VersionStamp?>
    {
        private readonly ISemanticChangeNotificationService _semanticChangeNotificationService;
        private readonly ClassificationTypeMap _typeMap;

        public override IEnumerable<Option<bool>> Options => SpecializedCollections.SingletonEnumerable(InternalFeatureOnOffOptions.SemanticColorizer);

        private IEditorClassificationService _classificationService;

        [ImportingConstructor]
        public SemanticClassificationTaggerProvider(
            IForegroundNotificationService notificationService,
            ISemanticChangeNotificationService semanticChangeNotificationService,
            ClassificationTypeMap typeMap,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
            : base(new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.Classification), notificationService)
        {
            _semanticChangeNotificationService = semanticChangeNotificationService;
            _typeMap = typeMap;
        }

        public override ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            return TaggerEventSources.Compose(
                TaggerEventSources.OnSemanticChanged(subjectBuffer, TaggerDelay.Short, _semanticChangeNotificationService),
                TaggerEventSources.OnDocumentActiveContextChanged(subjectBuffer, TaggerDelay.Short),
                TaggerEventSources.OnTextChanged(subjectBuffer, TaggerDelay.Short, reportChangedSpans: true));
        }

        public override async Task ProduceTagsAsync(Context context)
        {
            Debug.Assert(context.SpansToTag.IsSingle());
            Debug.Assert(context.CaretPosition == null);

            var spanToTag = context.SpansToTag.Single();
            var document = spanToTag.Document;

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

            var cancellationToken = context.CancellationToken;
            var newVersion = await document.Project.GetDependentSemanticVersionAsync(cancellationToken).ConfigureAwait(false);

            await ProduceTagsAsync(context, spanToTag, newVersion).ConfigureAwait(false);

            context.State = newVersion;
        }

        private async Task ProduceTagsAsync(Context context, DocumentSnapshotSpan spanToTag, VersionStamp newVersion)
        {
            if (await TryProduceTagsSpecializedAsync(context, spanToTag, newVersion).ConfigureAwait(false))
            {
                return;
            }

            // We weren't able to use our specialized codepaths for semantic classifying. 
            // Fall back to classifying the full span that was asked for.
            await ClassifySpansAsync(context, spanToTag).ConfigureAwait(false);
        }

        private Task<bool> TryProduceTagsSpecializedAsync(Context context, DocumentSnapshotSpan spanToTag, VersionStamp newVersion)
        {
            var range = context.TextChangeRange;

            if (range == null)
            {
                return Task.FromResult(TryProduceTagsWithNoTextChange(context, newVersion));
            }
            else
            {
                return TryProduceTagsWithTextChange(context, spanToTag, newVersion, range);
            }
        }

        private async Task<bool> TryProduceTagsWithTextChange(Context context, DocumentSnapshotSpan spanToTag, VersionStamp newVersion, TextChangeRange? range)
        {
            // there was top level edit, check whether that edit updated top level element
            var document = spanToTag.Document;
            var service = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
            var oldVersion = context.State;
            if (service == null || newVersion != oldVersion)
            {
                // There's been a version change since the last time we were called.  Reclassify 
                // everything.
                return false;
            }

            // perf optimization. Check whether all edits since the last update has happened within
            // a member. If it did, it will find the member that contains the changes and only refresh
            // that member.  If possible, try to get a speculative binder to make things even cheaper.

            var cancellationToken = context.CancellationToken;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var changedSpan = new TextSpan(range.Value.Span.Start, range.Value.NewLength);
            var member = service.GetContainingMemberDeclaration(root, changedSpan.Start);
            if (member == null || !member.FullSpan.Contains(changedSpan))
            {
                // The edit was not fully contained in a member.  Reclassify everything.
                return false;
            }

            var subTextSpan = service.GetMemberBodySpanForSpeculativeBinding(member);
            var subSpan = subTextSpan.Contains(changedSpan) ? subTextSpan.ToSpan() : member.FullSpan.ToSpan();

            var subSpanToTag = new DocumentSnapshotSpan(spanToTag.Document,
                new SnapshotSpan(spanToTag.SnapshotSpan.Snapshot, subSpan));

            // re-classify only the member we're inside.
            await ClassifySpansAsync(context, subSpanToTag).ConfigureAwait(false);
            return true;
        }

        private static bool TryProduceTagsWithNoTextChange(Context context, VersionStamp newVersion)
        {
            var oldVersion = context.State;

            // active file can be called twice for the same top level edit (the very last top
            // level edit).  One from text edit event source and one from semantic change event
            // source.  This make sure that when we are called to recompute due to semantic 
            // change event source, we haven't already recompute it by text edits event source.
            // for opened files that are not active, it should be called by semantic change 
            // event source and recompute tags for whole file.
            if (newVersion != oldVersion)
            {
                // There was no text change range specified.  And there was a version change,
                // we have to reclassify everything.
                return false;
            }

            // Mark that we didn't tag any spans.  That way we preserve all the old tags
            // from the last time we were called.
            context.SetSpansTagged(SpecializedCollections.EmptyEnumerable<DocumentSnapshotSpan>());

            // Return true so we do not do any further classification.
            return true;
        }

        private async Task ClassifySpansAsync(Context context, DocumentSnapshotSpan spanToTag)
        {
            try
            {
                var document = spanToTag.Document;
                var snapshotSpan = spanToTag.SnapshotSpan;
                var snapshot = snapshotSpan.Snapshot;

                // we don't directly reference the semantic model here, we just keep it alive so 
                // the classification service does not need to block to produce it.
                var cancellationToken = context.CancellationToken;
                using (Logger.LogBlock(FunctionId.Tagger_SemanticClassification_TagProducer_ProduceTags, cancellationToken))
                {
                    var textSpan = snapshotSpan.Span.ToTextSpan();
                    var extensionManager = document.Project.Solution.Workspace.Services.GetService<IExtensionManager>();

                    var classifiedSpans = ClassificationUtilities.GetOrCreateClassifiedSpanList();

                    await _classificationService.AddSemanticClassificationsAsync(
                        document, textSpan, classifiedSpans, cancellationToken: cancellationToken).ConfigureAwait(false);

                    ClassificationUtilities.Convert(_typeMap, snapshotSpan.Snapshot, classifiedSpans, context.AddTag);
                    ClassificationUtilities.ReturnClassifiedSpanList(classifiedSpans);

                    // Let the context know that this was the span we actually tried to tag.
                    context.SetSpansTagged(SpecializedCollections.SingletonEnumerable(spanToTag));
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }
    }
}
