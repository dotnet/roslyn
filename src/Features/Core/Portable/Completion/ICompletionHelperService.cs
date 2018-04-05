using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion
{
    internal interface ICompletionHelperService : IWorkspaceService
    {
        CompletionHelper GetCompletionHelper(Document document);
    }
}