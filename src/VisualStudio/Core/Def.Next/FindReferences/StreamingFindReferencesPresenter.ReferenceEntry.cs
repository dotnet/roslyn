// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.FindReferences;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;
using System.Windows.Media;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
using Microsoft.VisualStudio.Text.Editor;
using System.Linq;
using System.Windows.Controls;

namespace Microsoft.VisualStudio.LanguageServices.FindReferences
{
    internal partial class StreamingFindReferencesPresenter
    {
        private class ReferenceEntry
        {
            private readonly TableDataSourceFindReferencesContext _context;
            private readonly VisualStudioWorkspaceImpl _workspace;

            private readonly RoslynDefinitionBucket _definitionBucket;
            private readonly DocumentLocation _documentLocation;

            private readonly object _boxedProjectGuid;
            private readonly SourceText _sourceText;
            private readonly TaggedTextAndHighlightSpan _taggedLineParts;

            public ReferenceEntry(
                TableDataSourceFindReferencesContext context,
                VisualStudioWorkspaceImpl workspace,
                RoslynDefinitionBucket definitionBucket,
                DocumentLocation documentLocation,
                Guid projectGuid,
                SourceText sourceText,
                TaggedTextAndHighlightSpan taggedLineParts)
            {
                _context = context;

                _workspace = workspace;
                _definitionBucket = definitionBucket;
                _documentLocation = documentLocation;

                _boxedProjectGuid = projectGuid;
                _sourceText = sourceText;
                _taggedLineParts = taggedLineParts;
            }

            private StreamingFindReferencesPresenter Presenter => _context.Presenter;

            public bool TryGetValue(string keyName, out object content)
            {
                content = GetValue(keyName);
                return content != null;
            }

            private Document Document => _documentLocation.Document;
            private TextSpan SourceSpan => _documentLocation.SourceSpan;

            private object GetValue(string keyName)
            {
                switch (keyName)
                {
                case StandardTableKeyNames.DocumentName:
                    return Document.FilePath;
                    //var projectFilePath = Document.Project.FilePath;
                    //var documentPath = Document.FilePath;
                    //var projectDirectory = Path.GetDirectoryName(projectFilePath);

                    //if (documentPath.StartsWith(projectDirectory, StringComparison.OrdinalIgnoreCase))
                    //{
                    //    documentPath = documentPath.Substring(projectDirectory.Length);
                    //    documentPath = documentPath.TrimStart('\\', '/');
                    //}

                    //return documentPath;

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

                //case StandardTableKeyNames2.TextInlines:
                //    return GetHighlightedInlines(Presenter, _taggedLineParts);

                case StandardTableKeyNames2.DefinitionIcon:
                    return _definitionBucket.DefinitionItem.Tags.GetGlyph().GetImageMoniker();

                case StandardTableKeyNames2.Definition:
                    return _definitionBucket;
                }

                return null;
            }


            internal bool TryCreateColumnContent(string columnName, out FrameworkElement content)
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