using System;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.FindReferences
{
    internal partial class AsyncFindReferencesPresenter
    {
        private class TableEntry
        {
            private readonly INavigableItem _definition;
            private readonly INavigableItem _reference;
            private readonly object _boxedProjectGuid;
            private readonly SourceText _sourceText;

            public TableEntry(
                INavigableItem definition, INavigableItem reference, Guid projectGuid, SourceText sourceText)
            {
                _definition = definition;
                _reference = reference;
                _boxedProjectGuid = projectGuid;
                _sourceText = sourceText;
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
                    case StandardTableKeyNames.FullText:
                        return _sourceText.Lines.GetLineFromPosition(_reference.SourceSpan.Start).ToString().Trim();

                    case "namespace":
                        return "RoslynNS";
                }

                return null;
            }
        }
    }
}
