// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Roslyn.Utilities;

using CodeAnalysisQuickInfoItem = Microsoft.CodeAnalysis.QuickInfo.QuickInfoItem;
using IntellisenseQuickInfoItem = Microsoft.VisualStudio.Language.Intellisense.QuickInfoItem;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
{
    internal partial class Controller :
        AbstractController<Session<Controller, Model, IQuickInfoPresenterSession>, Model, IQuickInfoPresenterSession, IAsyncQuickInfoSession>
    {
        private static readonly object s_quickInfoPropertyKey = new object();
        private QuickInfoService _service;

        public Controller(
            ITextView textView,
            ITextBuffer subjectBuffer,
            IDocumentProvider documentProvider)
            : base(textView, subjectBuffer, null, null, documentProvider, "QuickInfo")
        {
        }

        // For testing purposes
        internal Controller(
            ITextView textView,
            ITextBuffer subjectBuffer,
            IDocumentProvider documentProvider,
            QuickInfoService service)
            : base(textView, subjectBuffer, null, null, documentProvider, "QuickInfo")
        {
            _service = service;
        }

        internal static Controller GetInstance(
            EditorCommandArgs args)
        {
            var textView = args.TextView;
            var subjectBuffer = args.SubjectBuffer;
            return textView.GetOrCreatePerSubjectBufferProperty(subjectBuffer, s_quickInfoPropertyKey,
                (v, b) => new Controller(v, b, new DocumentProvider()));
        }

        internal override void OnModelUpdated(Model modelOpt)
        {
            // do nothing
        }

        public async Task<IntellisenseQuickInfoItem> GetQuickInfoItemAsync(
            SnapshotPoint triggerPoint,
            CancellationToken cancellationToken)
        {
            var service = GetService();
            if (service == null)
            {
                return null;
            }

            var snapshot = this.SubjectBuffer.CurrentSnapshot;

            try
            {
                using (Logger.LogBlock(FunctionId.QuickInfo_ModelComputation_ComputeModelInBackground, cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var document = await DocumentProvider.GetDocumentAsync(snapshot, cancellationToken).ConfigureAwait(false);
                    if (document == null)
                    {
                        return null;
                    }

                    var item = await service.GetQuickInfoAsync(document, triggerPoint, cancellationToken).ConfigureAwait(false);
                    if (item != null)
                    {
                        return BuildIntellisenseQuickInfoItemAsync(triggerPoint, snapshot, item);
                    }

                    return null;
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        public QuickInfoService GetService()
        {
            if (_service == null)
            {
                var snapshot = this.SubjectBuffer.CurrentSnapshot;
                var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document != null)
                {
                    _service = QuickInfoService.GetService(document);
                }
            }

            return _service;
        }

        private IntellisenseQuickInfoItem BuildIntellisenseQuickInfoItemAsync(SnapshotPoint triggerPoint,
            ITextSnapshot snapshot, CodeAnalysisQuickInfoItem quickInfoItem)
        {
            var line = triggerPoint.GetContainingLine();
            var lineSpan = snapshot.CreateTrackingSpan(
                line.Extent,
                SpanTrackingMode.EdgeInclusive);

            var glyphs = quickInfoItem.Tags.GetGlyphs();

            //var symbolGlyph = glyphs.FirstOrDefault(g => g != Glyph.CompletionWarning);
            //var warningGlyph = glyphs.FirstOrDefault(g => g == Glyph.CompletionWarning);
            //var documentSpan = quickInfoItem.RelatedSpans.Length > 0 ?
            //CreateDocumentSpanPresentation(quickInfoItem, snapshot) : null;

            //var content = new QuickInfoDisplayPanel(
            //    symbolGlyph: symbolGlyph != default ? CreateSymbolPresentation(symbolGlyph) : null,
            //    warningGlyph: warningGlyph != default ? CreateSymbolPresentation(warningGlyph) : null,
            //    textBlocks: quickInfoItem.Sections.Select(section =>
            //        new TextBlockElement(section.Kind, CreateTextPresentation(section))).ToImmutableArray(),
            //    documentSpan: documentSpan);


            var textLines = new List<ClassifiedTextElement>();
            foreach(var section in quickInfoItem.Sections)
            {
                textLines.Add(new ClassifiedTextElement(section.TaggedParts.Select(
                    part => new ClassifiedTextRun(part.Tag.ToClassificationTypeName(), part.Text))));
            }

            var content = new ContainerElement(
                        ContainerElementStyle.Stacked,
                        textLines);

            return new IntellisenseQuickInfoItem(lineSpan, content);
        }

        //private FrameworkElement CreateSymbolPresentation(Glyph glyph)
        //{
        //    var image = new CrispImage
        //    {
        //        Moniker = glyph.GetImageMoniker()
        //    };

        //    // Inform the ImageService of the background color so that images have the correct background.
        //    var binding = new Binding("Background")
        //    {
        //        Converter = new BrushToColorConverter(),
        //        RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(QuickInfoDisplayPanel), 1)
        //    };

        //    image.SetBinding(ImageThemingUtilities.ImageBackgroundColorProperty, binding);
        //    return image;
        //}

        //private TextBlock CreateTextPresentation(QuickInfoSection section)
        //{
        //    if (section.Kind == QuickInfoSectionKinds.DocumentationComments)
        //    {
        //        return CreateDocumentationCommentPresentation(section.TaggedParts);
        //    }
        //    else
        //    {
        //        return CreateTextPresentation(section.TaggedParts);
        //    }
        //}

        //private TextBlock CreateTextPresentation(ImmutableArray<TaggedText> text)
        //{
        //    var formatMap = _classificationFormatMapService.GetClassificationFormatMap("tooltip");
        //    var classifiedTextBlock = text.ToTextBlock(formatMap, _classificationTypeMap);

        //    if (classifiedTextBlock.Inlines.Count == 0)
        //    {
        //        classifiedTextBlock.Visibility = Visibility.Collapsed;
        //    }

        //    return classifiedTextBlock;
        //}

        //private TextBlock CreateDocumentationCommentPresentation(ImmutableArray<TaggedText> text)
        //{
        //    var formatMap = _classificationFormatMapService.GetClassificationFormatMap("tooltip");
        //    var documentationTextBlock = text.ToTextBlock(formatMap, _classificationTypeMap);

        //    documentationTextBlock.TextWrapping = TextWrapping.Wrap;

        //    // If we have already computed the symbol documentation by now, update
        //    if (documentationTextBlock.Inlines.Count == 0)
        //    {
        //        documentationTextBlock.Visibility = Visibility.Collapsed;
        //    }

        //    return documentationTextBlock;
        //}

        //private FrameworkElement CreateDocumentSpanPresentation(Microsoft.CodeAnalysis.QuickInfo.QuickInfoItem info, ITextSnapshot snapshot)
        //{
        //    return ProjectionBufferContent.Create(
        //        info.RelatedSpans.Select(s => new SnapshotSpan(snapshot, new Span(s.Start, s.Length))).ToImmutableArray(),
        //        _projectionBufferFactoryService,
        //        _editorOptionsFactoryService,
        //        _textEditorFactoryService);
        //}
    }
}
