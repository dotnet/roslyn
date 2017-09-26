using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor
{
    interface ICaretIsInSnippetExpansionFieldService : IWorkspaceService
    {
        bool CaretIsInSnippetExpansionField(ITextView textView);
    }
}
