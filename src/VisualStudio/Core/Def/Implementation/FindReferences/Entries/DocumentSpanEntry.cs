// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.Editor.ReferenceHighlighting;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using DocumentHighlighting = Microsoft.CodeAnalysis.DocumentHighlighting;

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
            private readonly DocumentHighlighting.HighlightSpanKind _spanKind;
            private readonly ClassifiedSpansAndHighlightSpan _classifiedSpansAndHighlights;

            public DocumentSpanEntry(
                AbstractTableDataSourceFindUsagesContext context,
                RoslynDefinitionBucket definitionBucket,
                DocumentSpan documentSpan,
                DocumentHighlighting.HighlightSpanKind spanKind,
                string documentName,
                Guid projectGuid,
                SourceText sourceText,
                ClassifiedSpansAndHighlightSpan classifiedSpans)
                : base(context, definitionBucket, documentSpan, documentName, projectGuid, sourceText)
            {
                _spanKind = spanKind;
                _classifiedSpansAndHighlights = classifiedSpans;
            }

            protected override IList<System.Windows.Documents.Inline> CreateLineTextInlines()
            {
                var propertyId = _spanKind == DocumentHighlighting.HighlightSpanKind.Definition
                    ? DefinitionHighlightTag.TagId
                    : _spanKind == DocumentHighlighting.HighlightSpanKind.WrittenReference
                        ? WrittenReferenceHighlightTag.TagId
                        : ReferenceHighlightTag.TagId;

                var properties = Presenter.FormatMapService
                                          .GetEditorFormatMap("text")
                                          .GetProperties(propertyId);
                var highlightBrush = properties["Background"] as Brush;

                var classifiedSpans = _classifiedSpansAndHighlights.ClassifiedSpans;
                var classifiedTexts = classifiedSpans.SelectAsArray(
                    cs => new ClassifiedText(cs.ClassificationType, _sourceText.ToString(cs.TextSpan)));

                var inlines = classifiedTexts.ToInlines(
                    Presenter.TypeMap,
                    runCallback: (run, classifiedText, position) =>
                    {
                        if (highlightBrush != null)
                        {
                            if (position == _classifiedSpansAndHighlights.HighlightSpan.Start)
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
                    LazyToolTip.AttachTo(content, CreateDisposableToolTip);

                    return true;
                }

                return false;
            }

            private DisposableToolTip CreateDisposableToolTip()
            {
                Presenter.AssertIsForeground();

                // Create a new buffer that we'll show a preview for.  We can't search for an 
                // existing buffer because:
                //   1. the file may not be open.
                //   2. our results may not be in sync with what's actually in the editor.
                var textBuffer = CreateNewBuffer();

                // Create the actual tooltip around the region of that text buffer we want to show.
                var toolTip = new ToolTip
                {
                    Content = CreateToolTipContent(textBuffer),
                    Background = (Brush)Application.Current.Resources[EnvironmentColors.ToolWindowBackgroundBrushKey]
                };

                // Create a preview workspace for this text buffer and open it's corresponding 
                // document.  That way we'll get nice things like classification as well as the
                // reference highlight span.
                var newDocument = Document.WithText(textBuffer.AsTextContainer().CurrentText);
                var workspace = new PreviewWorkspace(newDocument.Project.Solution);
                workspace.OpenDocument(newDocument.Id);

                return new DisposableToolTip(toolTip, workspace);
            }

            private ContentControl CreateToolTipContent(ITextBuffer textBuffer)
            {
                var regionSpan = this.GetRegionSpanForReference();
                var snapshotSpan = textBuffer.CurrentSnapshot.GetSpan(regionSpan);

                var contentType = Presenter.ContentTypeRegistryService.GetContentType(
                    IProjectionBufferFactoryServiceExtensions.RoslynPreviewContentType);

                var roleSet = Presenter.TextEditorFactoryService.CreateTextViewRoleSet(
                    TextViewRoles.PreviewRole,
                    PredefinedTextViewRoles.Analyzable,
                    PredefinedTextViewRoles.Document,
                    PredefinedTextViewRoles.Editable);

                var content = new ProjectionBufferDeferredContent(
                    snapshotSpan,
                    Presenter.ProjectionBufferFactoryService,
                    Presenter.EditorOptionsFactoryService,
                    Presenter.TextEditorFactoryService,
                    contentType,
                    roleSet);

                var element = content.Create();
                return element;
            }

            private ITextBuffer CreateNewBuffer()
            {
                Presenter.AssertIsForeground();

                // is it okay to create buffer from threads other than UI thread?
                var contentTypeService = Document.Project.LanguageServices.GetService<IContentTypeLanguageService>();
                var contentType = contentTypeService.GetDefaultContentType();

                var textBuffer = Presenter.TextBufferFactoryService.CreateTextBuffer(
                    _sourceText.ToString(), contentType);

                // Create an appropriate highlight span on that buffer for the reference.
                var key = _spanKind == DocumentHighlighting.HighlightSpanKind.Definition
                    ? PredefinedPreviewTaggerKeys.DefinitionHighlightingSpansKey
                    : _spanKind == DocumentHighlighting.HighlightSpanKind.WrittenReference
                        ? PredefinedPreviewTaggerKeys.WrittenReferenceHighlightingSpansKey
                        : PredefinedPreviewTaggerKeys.ReferenceHighlightingSpansKey;
                textBuffer.Properties.RemoveProperty(key);
                textBuffer.Properties.AddProperty(key, new NormalizedSnapshotSpanCollection(
                    SourceSpan.ToSnapshotSpan(textBuffer.CurrentSnapshot)));

                return textBuffer;
            }

            private Span GetRegionSpanForReference()
            {
                const int AdditionalLineCountPerSide = 3;

                var referenceSpan = this.SourceSpan;
                var lineNumber = _sourceText.Lines.GetLineFromPosition(referenceSpan.Start).LineNumber;
                var firstLineNumber = Math.Max(0, lineNumber - AdditionalLineCountPerSide);
                var lastLineNumber = Math.Min(_sourceText.Lines.Count - 1, lineNumber + AdditionalLineCountPerSide);

                return Span.FromBounds(
                    _sourceText.Lines[firstLineNumber].Start,
                    _sourceText.Lines[lastLineNumber].End);
            }
        }
    }
}
