// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Editor.QuickInfo;
using Microsoft.CodeAnalysis.Editor.ReferenceHighlighting;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages
{
    internal partial class StreamingFindUsagesPresenter
    {
        /// <summary>
        /// Entry to show for a particular source location.  The row will show the classified
        /// contents of that line, and hovering will reveal a tooltip showing that line along
        /// with a few lines above/below it.
        /// </summary>
        private class DocumentSpanEntry : AbstractDocumentSpanEntry
        {
            private readonly HighlightSpanKind _spanKind;
            private readonly ExcerptResult _excerptResult;
            private readonly SymbolReferenceKinds _symbolReferenceKinds;
            private readonly ImmutableDictionary<string, string> _customColumnsData;

            public DocumentSpanEntry(
                AbstractTableDataSourceFindUsagesContext context,
                RoslynDefinitionBucket definitionBucket,
                HighlightSpanKind spanKind,
                string documentName,
                Guid projectGuid,
                MappedSpanResult mappedSpanResult,
                ExcerptResult excerptResult,
                SourceText lineText,
                SymbolUsageInfo symbolUsageInfo,
                ImmutableDictionary<string, string> customColumnsData)
                : base(context,
                      definitionBucket,
                      documentName,
                      projectGuid,
                      lineText,
                      mappedSpanResult)
            {
                _spanKind = spanKind;
                _excerptResult = excerptResult;
                _symbolReferenceKinds = symbolUsageInfo.ToSymbolReferenceKinds();
                _customColumnsData = customColumnsData;
            }

            protected override IList<System.Windows.Documents.Inline> CreateLineTextInlines()
            {
                var propertyId = _spanKind == HighlightSpanKind.Definition
                    ? DefinitionHighlightTag.TagId
                    : _spanKind == HighlightSpanKind.WrittenReference
                        ? WrittenReferenceHighlightTag.TagId
                        : ReferenceHighlightTag.TagId;

                var properties = Presenter.FormatMapService
                                          .GetEditorFormatMap("text")
                                          .GetProperties(propertyId);

                // Remove additive classified spans before creating classified text.
                // Otherwise the text will be repeated since there are two classifications
                // for the same span. Additive classifications should not change the foreground
                // color, so the resulting classified text will retain the proper look.
                var classifiedSpans = _excerptResult.ClassifiedSpans.WhereAsArray(
                    cs => !ClassificationTypeNames.AdditiveTypeNames.Contains(cs.ClassificationType));
                var classifiedTexts = classifiedSpans.SelectAsArray(
                    cs => new ClassifiedText(cs.ClassificationType, _excerptResult.Content.ToString(cs.TextSpan)));

                var inlines = classifiedTexts.ToInlines(
                    Presenter.ClassificationFormatMap,
                    Presenter.TypeMap,
                    runCallback: (run, classifiedText, position) =>
                    {
                        if (properties["Background"] is Brush highlightBrush)
                        {
                            if (position == _excerptResult.MappedSpan.Start)
                            {
                                run.SetValue(
                                    System.Windows.Documents.TextElement.BackgroundProperty,
                                    highlightBrush);
                            }
                        }
                    });

                return inlines;
            }

            public override bool TryCreateColumnContent(string columnName, out FrameworkElement content)
            {
                if (base.TryCreateColumnContent(columnName, out content))
                {
                    // this lazy tooltip causes whole solution to be kept in memory until find all reference result gets cleared.
                    // solution is never supposed to be kept alive for long time, meaning there is bunch of conditional weaktable or weak reference
                    // keyed by solution/project/document or corresponding states. this will cause all those to be kept alive in memory as well.
                    // probably we need to dig in to see how expensvie it is to support this
                    var controlService = _excerptResult.Document.Project.Solution.Workspace.Services.GetService<IContentControlService>();
                    controlService.AttachToolTipToControl(content, () =>
                        CreateDisposableToolTip(_excerptResult.Document, _excerptResult.Span));

                    return true;
                }

                return false;
            }

            protected override object GetValueWorker(string keyName)
            {
                if (keyName == StandardTableKeyNames2.SymbolKind)
                {
                    return _symbolReferenceKinds;
                }

                if (_customColumnsData.TryGetValue(keyName, out var value))
                {
                    return value;
                }

                return base.GetValueWorker(keyName);
            }

            private DisposableToolTip CreateDisposableToolTip(Document document, TextSpan sourceSpan)
            {
                Presenter.AssertIsForeground();

                var controlService = document.Project.Solution.Workspace.Services.GetService<IContentControlService>();
                var sourceText = document.GetTextSynchronously(CancellationToken.None);

                var excerptService = document.Services.GetService<IDocumentExcerptService>();
                if (excerptService != null)
                {
                    var excerpt = Presenter.ThreadingContext.JoinableTaskFactory.Run(() => excerptService.TryExcerptAsync(document, sourceSpan, ExcerptMode.Tooltip, CancellationToken.None));
                    if (excerpt != null)
                    {
                        // get tooltip from excerpt service
                        var clonedBuffer = excerpt.Value.Content.CreateTextBufferWithRoslynContentType(document.Project.Solution.Workspace);
                        SetHighlightSpan(_spanKind, clonedBuffer, excerpt.Value.MappedSpan);
                        SetStaticClassifications(clonedBuffer, excerpt.Value.ClassifiedSpans);

                        return controlService.CreateDisposableToolTip(clonedBuffer, EnvironmentColors.ToolWindowBackgroundBrushKey);
                    }
                }

                // get default behavior
                var textBuffer = document.CloneTextBuffer(sourceText);
                SetHighlightSpan(_spanKind, textBuffer, sourceSpan);

                var contentSpan = GetRegionSpanForReference(sourceText, sourceSpan);
                return controlService.CreateDisposableToolTip(document, textBuffer, contentSpan, EnvironmentColors.ToolWindowBackgroundBrushKey);
            }

            private void SetStaticClassifications(ITextBuffer textBuffer, ImmutableArray<ClassifiedSpan> classifiedSpans)
            {
                var key = PredefinedPreviewTaggerKeys.StaticClassificationSpansKey;
                textBuffer.Properties.RemoveProperty(key);
                textBuffer.Properties.AddProperty(key, classifiedSpans);
            }

            private static void SetHighlightSpan(HighlightSpanKind spanKind, ITextBuffer textBuffer, TextSpan span)
            {
                // Create an appropriate highlight span on that buffer for the reference.
                var key = spanKind == HighlightSpanKind.Definition
                    ? PredefinedPreviewTaggerKeys.DefinitionHighlightingSpansKey
                    : spanKind == HighlightSpanKind.WrittenReference
                        ? PredefinedPreviewTaggerKeys.WrittenReferenceHighlightingSpansKey
                        : PredefinedPreviewTaggerKeys.ReferenceHighlightingSpansKey;

                textBuffer.Properties.RemoveProperty(key);
                textBuffer.Properties.AddProperty(key, new NormalizedSnapshotSpanCollection(span.ToSnapshotSpan(textBuffer.CurrentSnapshot)));
            }

            private static Span GetRegionSpanForReference(SourceText sourceText, TextSpan sourceSpan)
            {
                const int AdditionalLineCountPerSide = 3;

                var referenceSpan = sourceSpan;
                var lineNumber = sourceText.Lines.GetLineFromPosition(referenceSpan.Start).LineNumber;
                var firstLineNumber = Math.Max(0, lineNumber - AdditionalLineCountPerSide);
                var lastLineNumber = Math.Min(sourceText.Lines.Count - 1, lineNumber + AdditionalLineCountPerSide);

                return Span.FromBounds(
                    sourceText.Lines[firstLineNumber].Start,
                    sourceText.Lines[lastLineNumber].End);
            }
        }
    }
}
