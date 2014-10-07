using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Services.Formatting;

namespace Roslyn.Services.Formatting.Options
{
    [ExportWorkspaceServiceFactory(typeof(IFormattingOptionsFactoryService), WorkspaceKind.Any)]
    internal sealed class DefaultFormattingOptionsFactoryServiceFactory : IWorkspaceServiceFactory
    {
        public DefaultFormattingOptionsFactoryServiceFactory()
        {
        }

        public IWorkspaceService CreateService(IWorkspaceServiceProvider workspaceServices)
        {
            return new Factory();
        }

        private sealed class Factory : IFormattingOptionsFactoryService
        {
            private static readonly FormattingOptions defaultFormattingOptions = FormattingOptions.GetDefaultOptions();

            public FormattingOptions GetFormattingOptions(Document document)
            {
                return defaultFormattingOptions;
            }
        }
    }
}
