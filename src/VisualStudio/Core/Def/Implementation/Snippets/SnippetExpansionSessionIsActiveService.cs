using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
{
    [Shared]
    [ExportWorkspaceService(typeof(ISnippetExpansionSessionIsActiveService), layer: ServiceLayer.Host)]
    internal class SnippetExpansionSessionIsActiveService : ISnippetExpansionSessionIsActiveService
    {
        public bool SnippetsAreActive(ITextView textView)
        {
            if (textView.Properties.TryGetProperty<AbstractSnippetExpansionClient>(typeof(AbstractSnippetExpansionClient), out var client))
            {
                return client.ExpansionSession != null;
            }

            return false;
        }
    }
}
