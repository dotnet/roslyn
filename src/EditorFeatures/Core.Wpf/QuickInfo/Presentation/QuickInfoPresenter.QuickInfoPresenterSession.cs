// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Wpf;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;

using QuickInfoItem = Microsoft.VisualStudio.Language.Intellisense.QuickInfoItem;
//using QuickInfoItem = Microsoft.CodeAnalysis.QuickInfo.QuickInfoItem;

#pragma warning disable CS0618 // IQuickInfo* is obsolete, tracked by https://github.com/dotnet/roslyn/issues/24094
namespace Microsoft.CodeAnalysis.Editor.QuickInfo.Presentation
{
    internal partial class QuickInfoPresenter
    {
        private class QuickInfoPresenterSession : ForegroundThreadAffinitizedObject, IQuickInfoPresenterSession
        {
            private readonly IAsyncQuickInfoBroker _quickInfoBroker;
            private readonly ITextView _textView;
            private readonly ITextBuffer _subjectBuffer;
            private readonly ClassificationTypeMap _classificationTypeMap;
            private readonly IClassificationFormatMapService _classificationFormatMapService;
            private readonly IProjectionBufferFactoryService _projectionBufferFactoryService;
            private readonly IEditorOptionsFactoryService _editorOptionsFactoryService;
            private readonly ITextEditorFactoryService _textEditorFactoryService;

            private IAsyncQuickInfoSession _editorSessionOpt;

            private QuickInfoItem _item;
            private ITrackingSpan _triggerSpan;

            public event EventHandler<EventArgs> Dismissed;

            public QuickInfoPresenterSession(
                IAsyncQuickInfoBroker quickInfoBroker,
                ITextView textView,
                ITextBuffer subjectBuffer,
                IAsyncQuickInfoSession sessionOpt,
                ClassificationTypeMap classificationTypeMap,
                IClassificationFormatMapService classificationFormatMapService,
                IProjectionBufferFactoryService projectionBufferFactoryService,
                IEditorOptionsFactoryService editorOptionsFactoryService,
                ITextEditorFactoryService textEditorFactoryService)
            {
                _quickInfoBroker = quickInfoBroker;
                _textView = textView;
                _subjectBuffer = subjectBuffer;
                _editorSessionOpt = sessionOpt;
                _classificationTypeMap = classificationTypeMap;
                _classificationFormatMapService = classificationFormatMapService;
                _projectionBufferFactoryService = projectionBufferFactoryService;
                _editorOptionsFactoryService = editorOptionsFactoryService;
                _textEditorFactoryService = textEditorFactoryService;
            }

            public void PresentItem(ITrackingSpan triggerSpan, QuickInfoItem item, bool trackMouse)
            {
                //AssertIsForeground();

                _triggerSpan = triggerSpan;
                _item = item;

                // It's a new list of items.  Either create the editor session if this is the first time, or ask the
                // editor session that we already have to recalculate.
                //if (_editorSessionOpt == null || _editorSessionOpt.IsDismissed)
                //{
                //    // We're tracking the caret.  Don't have the editor do it.
                //    var triggerPoint = triggerSpan.GetStartTrackingPoint(PointTrackingMode.Negative);

                //    _editorSessionOpt = _quickInfoBroker.CreateQuickInfoSession(_textView, triggerPoint, trackMouse: trackMouse);
                //    _editorSessionOpt.Dismissed += (s, e) => OnEditorSessionDismissed();
                //}

                // So here's the deal.  We cannot create the editor session and give it the right
                // signatures (even though we know what they are).  Instead, the session with
                // call back into the ISignatureHelpSourceProvider (which is us) to get those
                // values. It will pass itself along with the calls back into
                // ISignatureHelpSourceProvider. So, in order to make that connection work, we
                // add properties to the session so that we can call back into ourselves, get
                // the signatures and add it to the session.
                if (!_editorSessionOpt.Properties.ContainsProperty(s_augmentSessionKey))
                {
                    _editorSessionOpt.Properties.AddProperty(s_augmentSessionKey, this);
                }

                //_editorSessionOpt.Recalculate();
            }

            public void Dismiss()
            {
                //AssertIsForeground();

                if (_editorSessionOpt == null)
                {
                    // No editor session, nothing to do here.
                    return;
                }

                if (_item == null)
                {
                    // We don't have an item, so we're being asked to augment a session.
                    // Since we didn't put anything in the session, don't dismiss it either.
                    return;
                }

                //_editorSessionOpt.Dismiss();
                _editorSessionOpt = null;
            }

            private void OnEditorSessionDismissed()
            {
               // AssertIsForeground();
                this.Dismissed?.Invoke(this, EventArgs.Empty);
            }

            //internal void AugmentQuickInfoSession(IList<object> quickInfoContent, out ITrackingSpan applicableToSpan)
            //{
            //    applicableToSpan = _triggerSpan;

            //    var content = CreateContent(_item, _subjectBuffer.CurrentSnapshot);
            //    if (content != null)
            //    {
            //        quickInfoContent.Add(content);
            //    }
            //}

            public QuickInfoItem ConvertQuickInfoItem(ITextBuffer subjectBuffer, SnapshotPoint triggerPoint, Microsoft.CodeAnalysis.QuickInfo.QuickInfoItem quickInfoItem)
            {
                var line = triggerPoint.GetContainingLine();
                var lineNumber = triggerPoint.GetContainingLine().LineNumber;
                var lineSpan = subjectBuffer.CurrentSnapshot.CreateTrackingSpan(
                    line.Extent,
                    SpanTrackingMode.EdgeInclusive);


                var glyphs = quickInfoItem.Tags.GetGlyphs();
                var symbolGlyph = glyphs.FirstOrDefault(g => g != Glyph.CompletionWarning);
                var warningGlyph = glyphs.FirstOrDefault(g => g == Glyph.CompletionWarning);
                var documentSpan = quickInfoItem.RelatedSpans.Length > 0 ? CreateDocumentSpanPresentation(quickInfoItem, subjectBuffer.CurrentSnapshot) : null;

                var content = new QuickInfoDisplayPanel(
                    symbolGlyph: symbolGlyph != default ? CreateSymbolPresentation(symbolGlyph) : null,
                    warningGlyph: warningGlyph != default ? CreateSymbolPresentation(warningGlyph) : null,
                    textBlocks: quickInfoItem.Sections.Select(section => new TextBlockElement(section.Kind, CreateTextPresentation(section))).ToImmutableArray(),
                    documentSpan: documentSpan);

                return new QuickInfoItem(lineSpan, content);
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
#pragma warning restore CS0618 // IQuickInfo* is obsolete, tracked by https://github.com/dotnet/roslyn/issues/24094
