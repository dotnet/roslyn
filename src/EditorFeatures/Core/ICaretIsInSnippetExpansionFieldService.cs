using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface ICaretIsInSnippetExpansionFieldService : IWorkspaceService
    {
        bool CaretIsInSnippetExpansionField(ITextView textView);
    }
}
