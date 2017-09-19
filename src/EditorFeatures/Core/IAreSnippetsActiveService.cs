using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor
{
    interface ISnippetExpansionSessionIsActiveService : IWorkspaceService
    {
        bool SnippetsAreActive(ITextView textView);
    }
}
