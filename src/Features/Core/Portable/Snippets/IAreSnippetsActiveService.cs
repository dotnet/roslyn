using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Snippets
{
    interface ISnippetExpansionSessionIsActiveService : IWorkspaceService
    {
        bool SnippetsAreActive(Document document);
    }
}
