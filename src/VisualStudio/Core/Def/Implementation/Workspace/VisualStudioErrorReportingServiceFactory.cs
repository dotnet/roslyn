using System;
using System.Composition;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportWorkspaceServiceFactory(typeof(IErrorReportingService), ServiceLayer.Host), Shared]
    internal sealed class VisualStudioErrorReportingServiceFactory : IWorkspaceServiceFactory
    {
        private readonly VisualStudioWorkspaceImpl _workspace;
        private IErrorReportingService _singleton;

        [ImportingConstructor]
        public VisualStudioErrorReportingServiceFactory(VisualStudioWorkspaceImpl workspace)
        {
            _workspace = workspace;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return _singleton ?? (_singleton = new VisualStudioErrorReportingService(_workspace));
        }
    }
}
