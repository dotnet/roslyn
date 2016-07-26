using System;
using System.Collections.Immutable;
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

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal partial class StreamingFindReferencesPresenter
    {
        private class ReferenceEntry
        {
            private readonly StreamingFindReferencesPresenter _presenter;

            private readonly RoslynDefinitionBucket _definitionBucket;
            private readonly SourceReferenceItem _sourceReferenceItem;

            private readonly object _boxedProjectGuid;
            private readonly SourceText _sourceText;
            private readonly ImmutableArray<TaggedText> _taggedParts;

            public ReferenceEntry(
                StreamingFindReferencesPresenter presenter,
                RoslynDefinitionBucket definitionBucket,
                SourceReferenceItem sourceReferenceItem,
                Guid projectGuid,
                SourceText sourceText,
                ImmutableArray<TaggedText> taggedParts)
            {
                _presenter = presenter;

                _definitionBucket = definitionBucket;
                _sourceReferenceItem = sourceReferenceItem;

                _boxedProjectGuid = projectGuid;
                _sourceText = sourceText;
                _taggedParts = taggedParts;
            }

            public bool TryGetValue(string keyName, out object content)
            {
                content = GetValue(keyName);
                return content != null;
            }

            private DocumentLocation Location => _sourceReferenceItem.Location;
            private Document Document => Location.Document;
            private TextSpan SourceSpan => Location.SourceSpan;

            private object GetValue(string keyName)
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
                    return Location.Document.Project.Name;
                case StandardTableKeyNames.ProjectGuid:
                    return _boxedProjectGuid;

                case StandardTableKeyNames.Text:
                    return _sourceText.Lines.GetLineFromPosition(SourceSpan.Start).ToString().Trim();

                case StandardTableKeyNames.FullText:
                    // When we support classified lines, change this to:
                    // return GetEllisionBufferAroundReference();
                    return _sourceText.Lines.GetLineFromPosition(SourceSpan.Start).ToString().Trim();

                case StandardTableKeyNames2.TextInlines:
                    return _taggedParts.ToTextBlock(_presenter._typeMap).Inlines;

                //case StandardTableKeyNames2.DefinitionIcon:
                //    return  _displayGlyph ? (object)_item.Glyph.GetImageMoniker() : null;

                case StandardTableKeyNames2.Definition:
                    return _definitionBucket;
                }

                return null;
            }

            private FrameworkElement GetEllisionBufferAroundReference()
            {
                var snapshotSpan = GetSnapshotSpanAroundReference();
                var content = new ElisionBufferDeferredContent(
                    snapshotSpan,
                    _presenter._projectionBufferFactoryService,
                    _presenter._editorOptionsFactoryService,
                    _presenter._textEditorFactoryService);

                return content.Create();
            }

            private SnapshotSpan GetSnapshotSpanAroundReference()
            {
                var snapshot = GetTextSnapshot();

                var lines = _sourceText.Lines;
                var lineNumber = lines.GetLineFromPosition(SourceSpan.Start).LineNumber;
                var firstLineNumber = Math.Max(0, lineNumber - 2);
                var lastLineNumber = Math.Min(lines.Count - 1, lineNumber + 2);

                return new SnapshotSpan(snapshot,
                    Span.FromBounds(lines[firstLineNumber].Start, lines[lastLineNumber].End));
            }

            private ITextSnapshot GetTextSnapshot()
            {
                // Get the existing editor snapshot (if this is already open in an editor),
                // otherwise create a new snapshot that we can display.
                return _sourceText.FindCorrespondingEditorTextSnapshot() ?? CreateSnapshot();
            }

            private ITextSnapshot CreateSnapshot()
            {
                var contentTypeService = Document.GetLanguageService<IContentTypeLanguageService>();
                var contentType = contentTypeService.GetDefaultContentType();

                var textBuffer = _presenter._textBufferFactoryService.CreateTextBuffer(_sourceText.ToString(), contentType);
                return textBuffer.CurrentSnapshot;
            }
        }
    }
}