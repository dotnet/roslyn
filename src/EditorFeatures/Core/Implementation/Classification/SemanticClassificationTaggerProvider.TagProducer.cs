// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Classification
{
    internal partial class SemanticClassificationTaggerProvider
    {
        private class TagProducer : AbstractSingleDocumentTagProducer<IClassificationTag>
        {
            private readonly ClassificationTypeMap _typeMap;
            private IEditorClassificationService _classificationService;

            public TagProducer(ClassificationTypeMap typeMap)
            {
                _typeMap = typeMap;
            }

            public override async Task<IEnumerable<ITagSpan<IClassificationTag>>> ProduceTagsAsync(
                Document document,
                SnapshotSpan snapshotSpan,
                int? caretPosition,
                CancellationToken cancellationToken)
            {
                try
                {
                    var snapshot = snapshotSpan.Snapshot;
                    if (document == null)
                    {
                        return SpecializedCollections.EmptyEnumerable<ITagSpan<IClassificationTag>>();
                    }

                    if (_classificationService == null)
                    {
                        _classificationService = document.Project.LanguageServices.GetService<IEditorClassificationService>();
                    }

                    if (_classificationService == null)
                    {
                        return SpecializedCollections.EmptyEnumerable<ITagSpan<IClassificationTag>>();
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

                        return ClassificationUtilities.ConvertAndReturnList(_typeMap, snapshotSpan.Snapshot, classifiedSpans);
                    }
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }
        }
    }
}
