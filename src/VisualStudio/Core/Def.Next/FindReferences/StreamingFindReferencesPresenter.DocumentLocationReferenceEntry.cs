using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
using Microsoft.CodeAnalysis.FindReferences;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.LanguageServices.FindReferences
{
    internal partial class StreamingFindReferencesPresenter
    {
        private class DocumentLocationReferenceEntry : ReferenceEntry
        {
            private readonly TableDataSourceFindReferencesContext _context;
            private readonly VisualStudioWorkspaceImpl _workspace;

            private readonly DocumentLocation _documentLocation;

            private readonly object _boxedProjectGuid;
            private readonly SourceText _sourceText;
            private readonly TaggedTextAndHighlightSpan _taggedLineParts;

            public DocumentLocationReferenceEntry(
                TableDataSourceFindReferencesContext context,
                VisualStudioWorkspaceImpl workspace,
                RoslynDefinitionBucket definitionBucket,
                DocumentLocation documentLocation,
                Guid projectGuid,
                SourceText sourceText,
                TaggedTextAndHighlightSpan taggedLineParts)
                : base(definitionBucket)
            {
                _context = context;

                _workspace = workspace;
                _documentLocation = documentLocation;

                _boxedProjectGuid = projectGuid;
                _sourceText = sourceText;
                _taggedLineParts = taggedLineParts;
            }

            private StreamingFindReferencesPresenter Presenter => _context.Presenter;

            private Document Document => _documentLocation.Document;
            private TextSpan SourceSpan => _documentLocation.SourceSpan;

            protected override object GetValueWorker(string keyName)
            {
                switch (keyName)
                {
                case StandardTableKeyNames.DocumentName:
                    return Document.FilePath;
                case StandardTableKeyNames.Line:
                    return _sourceText.Lines.GetLinePosition(SourceSpan.Start).Line;
                case StandardTableKeyNames.Column:
                    return _sourceText.Lines.GetLinePosition(SourceSpan.Start).Character;
                case StandardTableKeyNames.ProjectName:
                    return Document.Project.Name;
                case StandardTableKeyNames.ProjectGuid:
                    return _boxedProjectGuid;
                case StandardTableKeyNames.Text:
                    return _sourceText.Lines.GetLineFromPosition(SourceSpan.Start).ToString().Trim();
                }

                return null;
            }

            public override bool TryCreateColumnContent(string columnName, out FrameworkElement content)
            {
                if (columnName == StandardTableColumnDefinitions2.LineText)
                {
                    var inlines = GetHighlightedInlines(Presenter, _taggedLineParts);
                    var textBlock = inlines.ToTextBlock(Presenter._typeMap);

                    textBlock.ToolTip = new LazyTip(textBlock,
                        () => CreateToolTip(),
                        () => SetHighlightSpansOnBuffer());

                    content = textBlock;
                    return true;
                }

                content = null;
                return false;
            }


            private static IList<System.Windows.Documents.Inline> GetHighlightedInlines(
                StreamingFindReferencesPresenter presenter,
                TaggedTextAndHighlightSpan taggedTextAndHighlight,
                string classificationFormatMap = null)
            {
                var properties = presenter._formatMapService
                                          .GetEditorFormatMap("text")
                                          .GetProperties("MarkerFormatDefinition/HighlightedReference");
                var highlightBrush = properties["Background"] as Brush;

                var lineParts = taggedTextAndHighlight.TaggedText;
                var inlines = lineParts.ToInlines(
                    presenter._typeMap,
                    classificationFormatMap,
                    (run, taggedText, position) =>
                    {
                        if (highlightBrush != null)
                        {
                            if (position == taggedTextAndHighlight.HighlightSpan.Start)
                            {
                                run.SetValue(
                                    System.Windows.Documents.TextElement.BackgroundProperty,
                                    highlightBrush);
                            }
                        }
                    });

                return inlines;
            }

            private ToolTip CreateToolTip()
            {
                var content = CreateToolTipContent();

                var toolTip = new ToolTip { Content = content };

                var style = Presenter._presenterStyles.FirstOrDefault(s => !string.IsNullOrEmpty(s.QuickInfoAppearanceCategory));
                if (style?.BackgroundBrush != null)
                {
                    toolTip.Background = style.BackgroundBrush;
                }

                return toolTip;
            }

            private ContentControl CreateToolTipContent()
            {
                var textBuffer = _context.GetTextBufferForPreview(Document, _sourceText);

                SetHighlightSpansOnBuffer();

                var regionSpan = this.GetRegionSpanForReference();
                var snapshotSpan = textBuffer.CurrentSnapshot.GetSpan(regionSpan);

                var contentType = Presenter._contentTypeRegistryService.GetContentType(
                    IProjectionBufferFactoryServiceExtensions.RoslynPreviewContentType);

                var roleSet = Presenter._textEditorFactoryService.CreateTextViewRoleSet(
                    TextViewRoles.PreviewRole,
                    PredefinedTextViewRoles.Analyzable,
                    PredefinedTextViewRoles.Document,
                    PredefinedTextViewRoles.Editable);

                var content = new ElisionBufferDeferredContent(
                    snapshotSpan,
                    Presenter._projectionBufferFactoryService,
                    Presenter._editorOptionsFactoryService,
                    Presenter._textEditorFactoryService,
                    contentType,
                    roleSet);

                var element = content.Create();

                return element;
            }

            private void SetHighlightSpansOnBuffer()
            {
                var textBuffer = _context.GetTextBufferForPreview(Document, _sourceText);

                var key = PredefinedPreviewTaggerKeys.ReferenceHighlightingSpansKey;
                textBuffer.Properties.RemoveProperty(key);
                textBuffer.Properties.AddProperty(key, new NormalizedSnapshotSpanCollection(
                    SourceSpan.ToSnapshotSpan(textBuffer.CurrentSnapshot)));
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
