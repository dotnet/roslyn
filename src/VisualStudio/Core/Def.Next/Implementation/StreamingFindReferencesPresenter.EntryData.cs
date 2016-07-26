using System;
using System.Collections.Immutable;
using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal partial class StreamingFindReferencesPresenter
    {
        /// <summary>
        /// Stores and provides access to data that is common to navigable items (whether they are
        /// references or definitions).
        /// </summary>
        private class NavigableItemEntryData
        {
            private readonly StreamingFindReferencesPresenter _presenter;

            private readonly INavigableItem _item;
            private readonly object _boxedProjectGuid;
            private readonly SourceText _sourceText;
            private readonly ImmutableArray<TaggedText> _taggedParts;
            private readonly bool _displayGlyph;

            public NavigableItemEntryData(
                StreamingFindReferencesPresenter presenter,
                INavigableItem item,
                Guid projectGuid,
                SourceText sourceText,
                ImmutableArray<TaggedText> taggedParts,
                bool displayGlyph)
            {
                _presenter = presenter;
                _item = item;
                _boxedProjectGuid = projectGuid;
                _sourceText = sourceText;
                _taggedParts = taggedParts;
                _displayGlyph = displayGlyph;
            }

            public object GetValue(string keyName)
            {
                switch (keyName)
                {
                    case StandardTableKeyNames.DocumentName:
                        return _item.Document.FilePath;
                    case StandardTableKeyNames.Line:
                        return _sourceText.Lines.GetLinePosition(_item.SourceSpan.Start).Line;
                    case StandardTableKeyNames.Column:
                        return _sourceText.Lines.GetLinePosition(_item.SourceSpan.Start).Character;
                    case StandardTableKeyNames.ProjectName:
                        return _item.Document.Project.Name;
                    case StandardTableKeyNames.ProjectGuid:
                        return _boxedProjectGuid;

                    case StandardTableKeyNames.Text:
                        return _sourceText.Lines.GetLineFromPosition(_item.SourceSpan.Start).ToString().Trim();

                    case StandardTableKeyNames.FullText:
                        // When we support classified lines, change this to:
                        // return GetEllisionBufferAroundReference();
                        return _sourceText.Lines.GetLineFromPosition(_item.SourceSpan.Start).ToString().Trim();

                    case StandardTableKeyNames2.TextInlines:
                        return _taggedParts.ToTextBlock(_presenter._typeMap).Inlines;

                    case StandardTableKeyNames2.DefinitionIcon:
                        return _displayGlyph ? (object)_item.Glyph.GetImageMoniker() : null;
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
                var lineNumber = lines.GetLineFromPosition(_item.SourceSpan.Start).LineNumber;
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
                var contentTypeService = _item.Document.GetLanguageService<IContentTypeLanguageService>();
                var contentType = contentTypeService.GetDefaultContentType();

                var textBuffer = _presenter._textBufferFactoryService.CreateTextBuffer(_sourceText.ToString(), contentType);
                return textBuffer.CurrentSnapshot;
            }
        }
    }
}