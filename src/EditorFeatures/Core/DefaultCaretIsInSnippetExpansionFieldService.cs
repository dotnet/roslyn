using System.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Snippets
{
    [Shared]
    [ExportWorkspaceService(typeof(ICaretIsInSnippetExpansionFieldService), ServiceLayer.Default)]
    internal class DefaultSnippetExpansionSessionIsActiveService : ICaretIsInSnippetExpansionFieldService
    {
        public bool CaretIsInSnippetExpansionField(ITextView textView) => false;
    }
}
