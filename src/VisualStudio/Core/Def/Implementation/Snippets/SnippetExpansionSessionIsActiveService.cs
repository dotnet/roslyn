using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
{
    [Shared]
    [ExportWorkspaceService(typeof(ISnippetExpansionSessionIsActiveService), layer: ServiceLayer.Host)]
    internal class SnippetExpansionSessionIsActiveService : ISnippetExpansionSessionIsActiveService
    {
        IVsEditorAdaptersFactoryService _adapterFactory;
        IVsTextManager _textManager;

        [ImportingConstructor]
        internal SnippetExpansionSessionIsActiveService(SVsServiceProvider serviceProvider , IVsEditorAdaptersFactoryService adapterFactory)
        {
            _adapterFactory = adapterFactory;
            _textManager = (IVsTextManager)serviceProvider.GetService(typeof(SVsTextManager));
        }

        public bool SnippetsAreActive(Document document)
        {
            if (document.TryGetText(out var text))
            {
                var buffer = text.Container.GetTextBuffer();
                if (buffer != null)
                {
                    var bufferAdapter = _adapterFactory.GetBufferAdapter(buffer);
                    if (_textManager.GetActiveView(1,  bufferAdapter, out var viewAdapter) == VSConstants.S_OK)
                    {
                        var wpfView = _adapterFactory.GetWpfTextView(viewAdapter);

                        if (wpfView.Properties.TryGetProperty<AbstractSnippetExpansionClient>(typeof(AbstractSnippetExpansionClient), out var client))
                        {
                            return client.ExpansionSession != null;
                        }
                    }
                }
            }

            return false;
        }
    }
}
