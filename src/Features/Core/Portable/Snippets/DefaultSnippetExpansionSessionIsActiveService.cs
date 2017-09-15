using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Snippets
{
    [Shared]
    [ExportWorkspaceService(typeof(ISnippetExpansionSessionIsActiveService), layer: ServiceLayer.Default)]
    class DefaultSnippetExpansionSessionIsActiveService : ISnippetExpansionSessionIsActiveService
    {
        public bool SnippetsAreActive(Document document) => false;
    }
}
