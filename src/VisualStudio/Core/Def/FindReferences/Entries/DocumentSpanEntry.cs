// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages;

internal partial class StreamingFindUsagesPresenter
{
    /// <summary>
    /// Entry to show for a particular source location.  The row will show the classified
    /// contents of that line, and hovering will reveal a tooltip showing that line along
    /// with a few lines above/below it.
    /// </summary>
    private sealed class DocumentSpanEntry : AbstractDocumentSpanEntry
    {
        private readonly HighlightSpanKind _spanKind;
        private readonly ExcerptResult _excerptResult;
        private readonly SymbolReferenceKinds _symbolReferenceKinds;
        private readonly ImmutableArray<(string key, string value)> _customColumnsData;

        private DocumentSpanEntry(
            AbstractTableDataSourceFindUsagesContext context,
            RoslynDefinitionBucket definitionBucket,
            Guid projectGuid,
            string projectName,
            HighlightSpanKind spanKind,
            MappedSpanResult mappedSpanResult,
            ExcerptResult excerptResult,
            SourceText lineText,
            SymbolUsageInfo symbolUsageInfo,
            ImmutableArray<(string key, string value)> customColumnsData,
            IThreadingContext threadingContext)
            : base(context, definitionBucket, projectGuid, projectName, lineText, mappedSpanResult, threadingContext)
        {
            _spanKind = spanKind;
            _excerptResult = excerptResult;
            _symbolReferenceKinds = symbolUsageInfo.ToSymbolReferenceKinds();
            _customColumnsData = customColumnsData;
        }

        protected override Document Document
            => _excerptResult.Document;

        protected override TextSpan NavigateToTargetSpan
            => _excerptResult.Span;

        public static DocumentSpanEntry? TryCreate(
            AbstractTableDataSourceFindUsagesContext context,
            RoslynDefinitionBucket definitionBucket,
            Guid guid,
            string projectName,
            string? filePath,
            TextSpan sourceSpan,
            HighlightSpanKind spanKind,
            MappedSpanResult mappedSpanResult,
            ExcerptResult excerptResult,
            SourceText lineText,
            SymbolUsageInfo symbolUsageInfo,
            ImmutableArray<(string key, string value)> customColumnsData,
            IThreadingContext threadingContext)
        {
            var entry = new DocumentSpanEntry(
                context, definitionBucket, guid, projectName, spanKind, mappedSpanResult, excerptResult,
                lineText, symbolUsageInfo, customColumnsData, threadingContext);

            // Because of things like linked files, we may have a reference up in multiple different locations that are
            // effectively at the exact same navigation location for the user. i.e. they're the same file/span.  Showing
            // multiple entries for these is just noisy and gets worse and worse with shared projects and whatnot.  So,
            // we collapse things down to only show a single entry for each unique file/span pair.
            var winningEntry = definitionBucket.GetOrAddEntry(filePath, sourceSpan, entry);

            // If we were the one that successfully added this entry to the bucket, then pass us back out to be put in
            // the ui.
            if (winningEntry == entry)
                return entry;

            // We were not the winner.  Throw away the item we created as we do not want to add it to the ui.
            return null;
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

        public override bool TryCreateColumnContent(string columnName, [NotNullWhen(true)] out FrameworkElement? content)
        {
            if (base.TryCreateColumnContent(columnName, out content))
            {
                // this lazy tooltip causes whole solution to be kept in memory until find all reference result gets cleared.
                // solution is never supposed to be kept alive for long time, meaning there is bunch of conditional weaktable or weak reference
                // keyed by solution/project/document or corresponding states. this will cause all those to be kept alive in memory as well.
                // probably we need to dig in to see how expensvie it is to support this
                var controlService = _excerptResult.Document.Project.Solution.Services.GetRequiredService<IContentControlService>();
                controlService.AttachToolTipToControl(content, () =>
                    CreateDisposableToolTip(_excerptResult.Document, _excerptResult.Span));

                return true;
            }

            return false;
        }

        protected override object? GetValueWorker(string keyName)
        {
            if (keyName == StandardTableKeyNames2.SymbolKind)
            {
                return _symbolReferenceKinds;
            }

            foreach (var (key, value) in _customColumnsData)
            {
                if (key == keyName)
                    return value;
            }

            return base.GetValueWorker(keyName);
        }

        private DisposableToolTip CreateDisposableToolTip(Document document, TextSpan sourceSpan)
        {
            this.Presenter.ThreadingContext.ThrowIfNotOnUIThread();

            var controlService = document.Project.Solution.Services.GetRequiredService<IContentControlService>();
            var sourceText = document.GetTextSynchronously(CancellationToken.None);

            var excerptService = document.DocumentServiceProvider.GetService<IDocumentExcerptService>();
            if (excerptService != null)
            {
                var classificationOptions = Presenter._globalOptions.GetClassificationOptions(document.Project.Language);
                var excerpt = this.Presenter.ThreadingContext.JoinableTaskFactory.Run(() => excerptService.TryExcerptAsync(document, sourceSpan, ExcerptMode.Tooltip, classificationOptions, CancellationToken.None));
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

        private static void SetStaticClassifications(ITextBuffer textBuffer, ImmutableArray<ClassifiedSpan> classifiedSpans)
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
