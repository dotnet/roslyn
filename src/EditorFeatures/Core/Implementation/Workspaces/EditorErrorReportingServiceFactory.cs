using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Workspaces
{
    [ExportWorkspaceServiceFactory(typeof(IErrorReportingService), ServiceLayer.Editor), Shared]
    class EditorErrorReportingServiceFactory : IWorkspaceServiceFactory
    {
        private Lazy<IErrorReportingService> _singleton = new Lazy<IErrorReportingService>(() => new EditorErrorReportingService());

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return _singleton.Value;
        }
    }
}
