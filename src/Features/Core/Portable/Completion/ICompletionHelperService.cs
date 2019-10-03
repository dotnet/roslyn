using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Completion
{
    internal interface ICompletionHelperService : IWorkspaceService
    {
        CompletionHelper GetCompletionHelper(Document document);
    }
}
