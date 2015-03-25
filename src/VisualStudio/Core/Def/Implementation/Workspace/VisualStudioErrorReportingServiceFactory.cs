using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportWorkspaceServiceFactory(typeof(IErrorReportingService), ServiceLayer.Host), Shared]
    internal sealed class VisualStudioErrorReportingServiceFactory : IWorkspaceServiceFactory
    {
        private Lazy<IErrorReportingService> _singleton;

        [ImportingConstructor]
        public VisualStudioErrorReportingServiceFactory(VisualStudioWorkspaceImpl workspace, IForegroundNotificationService foregroundNotificationService)
        {
            _singleton = new Lazy<IErrorReportingService>(() => new VisualStudioErrorReportingService(workspace, foregroundNotificationService));
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return _singleton.Value;
        }
    }
}
