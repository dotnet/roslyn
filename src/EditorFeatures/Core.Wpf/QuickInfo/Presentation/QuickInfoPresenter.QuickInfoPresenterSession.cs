// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Wpf;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;

using IntellisenseQuickInfoItem = Microsoft.VisualStudio.Language.Intellisense.QuickInfoItem;
using CodeAnalysisQuickInfoItem = Microsoft.CodeAnalysis.QuickInfo.QuickInfoItem;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo.Presentation
{
    internal partial class QuickInfoPresenter
    {
        private class QuickInfoPresenterSession : ForegroundThreadAffinitizedObject, IQuickInfoPresenterSession
        {
            private readonly ITextView _textView;
            private readonly ITextBuffer _subjectBuffer;
            private readonly ClassificationTypeMap _classificationTypeMap;
            private readonly IClassificationFormatMapService _classificationFormatMapService;
            private readonly IProjectionBufferFactoryService _projectionBufferFactoryService;
            private readonly IEditorOptionsFactoryService _editorOptionsFactoryService;
            private readonly ITextEditorFactoryService _textEditorFactoryService;

            public event EventHandler<EventArgs> Dismissed;

            public QuickInfoPresenterSession(
                ITextView textView,
                ITextBuffer subjectBuffer,
                ClassificationTypeMap classificationTypeMap,
                IClassificationFormatMapService classificationFormatMapService,
                IProjectionBufferFactoryService projectionBufferFactoryService,
                IEditorOptionsFactoryService editorOptionsFactoryService,
                ITextEditorFactoryService textEditorFactoryService)
            {
                _textView = textView;
                _subjectBuffer = subjectBuffer;
                _classificationTypeMap = classificationTypeMap;
                _classificationFormatMapService = classificationFormatMapService;
                _projectionBufferFactoryService = projectionBufferFactoryService;
                _editorOptionsFactoryService = editorOptionsFactoryService;
                _textEditorFactoryService = textEditorFactoryService;
            }

            public void Dismiss()
            {
                // do nothing
            }

            public async Task<IntellisenseQuickInfoItem> BuildIntellisenseQuickInfoItemAsync(SnapshotPoint triggerPoint, CodeAnalysisQuickInfoItem quickInfoItem)
            {
                IntellisenseQuickInfoItem item = null;
                await InvokeBelowInputPriority(() =>
                {
                    var line = triggerPoint.GetContainingLine();
                    var lineNumber = triggerPoint.GetContainingLine().LineNumber;
                    var lineSpan = this._subjectBuffer.CurrentSnapshot.CreateTrackingSpan(
                        line.Extent,
                        SpanTrackingMode.EdgeInclusive);


                    var glyphs = quickInfoItem.Tags.GetGlyphs();
                    var symbolGlyph = glyphs.FirstOrDefault(g => g != Glyph.CompletionWarning);
                    var warningGlyph = glyphs.FirstOrDefault(g => g == Glyph.CompletionWarning);
                    var documentSpan = quickInfoItem.RelatedSpans.Length > 0 ?
                        CreateDocumentSpanPresentation(quickInfoItem, this._subjectBuffer.CurrentSnapshot) : null;

                    var content = new QuickInfoDisplayPanel(
                        symbolGlyph: symbolGlyph != default ? CreateSymbolPresentation(symbolGlyph) : null,
                        warningGlyph: warningGlyph != default ? CreateSymbolPresentation(warningGlyph) : null,
                        textBlocks: quickInfoItem.Sections.Select(section =>
                            new TextBlockElement(section.Kind, CreateTextPresentation(section))).ToImmutableArray(),
                        documentSpan: documentSpan);

                    item = new IntellisenseQuickInfoItem(lineSpan, content);
                }).ConfigureAwait(false);

                return item;
            }

            private FrameworkElement CreateSymbolPresentation(Glyph glyph)
            {
                var image = new CrispImage
                {
                    Moniker = glyph.GetImageMoniker()
                };

                // Inform the ImageService of the background color so that images have the correct background.
                var binding = new Binding("Background")
                {
                    Converter = new BrushToColorConverter(),
                    RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(QuickInfoDisplayPanel), 1)
                };

                image.SetBinding(ImageThemingUtilities.ImageBackgroundColorProperty, binding);
                return image;
            }

            private TextBlock CreateTextPresentation(QuickInfoSection section)
            {
                if (section.Kind == QuickInfoSectionKinds.DocumentationComments)
                {
                    return CreateDocumentationCommentPresentation(section.TaggedParts);
                }
                else
                {
                    return CreateTextPresentation(section.TaggedParts);
                }
            }

            private TextBlock CreateTextPresentation(ImmutableArray<TaggedText> text)
            {
                var formatMap = _classificationFormatMapService.GetClassificationFormatMap("tooltip");
                var classifiedTextBlock = text.ToTextBlock(formatMap, _classificationTypeMap);

                if (classifiedTextBlock.Inlines.Count == 0)
                {
                    classifiedTextBlock.Visibility = Visibility.Collapsed;
                }

                return classifiedTextBlock;
            }

            private TextBlock CreateDocumentationCommentPresentation(ImmutableArray<TaggedText> text)
            {
                var formatMap = _classificationFormatMapService.GetClassificationFormatMap("tooltip");
                var documentationTextBlock = text.ToTextBlock(formatMap, _classificationTypeMap);

                documentationTextBlock.TextWrapping = TextWrapping.Wrap;

                // If we have already computed the symbol documentation by now, update
                if (documentationTextBlock.Inlines.Count == 0)
                {
                    documentationTextBlock.Visibility = Visibility.Collapsed;
                }

                return documentationTextBlock;
            }

            private FrameworkElement CreateDocumentSpanPresentation(Microsoft.CodeAnalysis.QuickInfo.QuickInfoItem info, ITextSnapshot snapshot)
            {
                return ProjectionBufferContent.Create(
                    info.RelatedSpans.Select(s => new SnapshotSpan(snapshot, new Span(s.Start, s.Length))).ToImmutableArray(),
                    _projectionBufferFactoryService,
                    _editorOptionsFactoryService,
                    _textEditorFactoryService);
            }
        }
    }
}
