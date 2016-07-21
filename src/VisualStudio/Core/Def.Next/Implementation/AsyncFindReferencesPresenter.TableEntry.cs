using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal partial class AsyncFindReferencesPresenter
    {
        private class TableEntry
        {
            private readonly AsyncFindReferencesPresenter _presenter;

            private readonly INavigableItem _definition;
            private readonly INavigableItem _reference;
            private readonly object _boxedProjectGuid;
            private readonly SourceText _sourceText;
            private readonly List<SymbolDisplayPart> _classifiedLineParts;

            public TableEntry(
                AsyncFindReferencesPresenter presenter,
                INavigableItem definition, INavigableItem reference, Guid projectGuid, SourceText sourceText,
                List<SymbolDisplayPart> classifiedLineParts)
            {
                _presenter = presenter;
                _definition = definition;
                _reference = reference;
                _boxedProjectGuid = projectGuid;
                _sourceText = sourceText;
                _classifiedLineParts = classifiedLineParts;
            }

            public bool TryGetValue(string keyName, out object content)
            {
                content = GetValue(keyName);
                return content != null;
            }

            private object GetValue(string keyName)
            {
                switch (keyName)
                {
                    case StandardTableKeyNames.DocumentName:
                        return _reference.Document.FilePath;
                    case StandardTableKeyNames.Line:
                        return _sourceText.Lines.GetLinePosition(_reference.SourceSpan.Start).Line;
                    case StandardTableKeyNames.Column:
                        return _sourceText.Lines.GetLinePosition(_reference.SourceSpan.Start).Character;
                    case StandardTableKeyNames.ProjectName:
                        return _reference.Document.Project.Name;
                    case StandardTableKeyNames.ProjectGuid:
                        return _boxedProjectGuid;
                    case StandardTableKeyNames.Text:
                        // When we support classified lines, change this to:
                        // return _classifiedLineParts.ToTextBlock(_presenter._typeMap);
                        return _sourceText.Lines.GetLineFromPosition(_reference.SourceSpan.Start).ToString().Trim();

                    case StandardTableKeyNames.FullText:
                        // When we support classified lines, change this to:
                        // return GetEllisionBufferAroundReference();
                        return _sourceText.Lines.GetLineFromPosition(_reference.SourceSpan.Start).ToString().Trim();

                    case "namespace":
                        return "RoslynNS";
                }

                return null;
            }

            private object GetEllisionBufferAroundReference()
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
                var existingSnaphot = _sourceText.FindCorrespondingEditorTextSnapshot();

                var lines = _sourceText.Lines;
                var lineNumber = lines.GetLineFromPosition(_reference.SourceSpan.Start).LineNumber;
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
                var contentTypeService = _reference.Document.GetLanguageService<IContentTypeLanguageService>();
                var contentType = contentTypeService.GetDefaultContentType();

                var textBuffer = _presenter._textBufferFactoryService.CreateTextBuffer(_sourceText.ToString(), contentType);
                return textBuffer.CurrentSnapshot;
            }
        }
    }
}