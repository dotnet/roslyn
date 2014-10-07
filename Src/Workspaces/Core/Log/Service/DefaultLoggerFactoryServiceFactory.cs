using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.WorkspaceServices;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// workspace service that will return default implementation of logger
    /// </summary>
#if MEF 
    [ExportWorkspaceServiceFactory(typeof(ILoggerFactoryService), WorkspaceKind.Any)]
#endif
    internal sealed class DefaultLoggerFactoryServiceFactory : IWorkspaceServiceFactory
    {
        public DefaultLoggerFactoryServiceFactory()
        {
        }

        public IWorkspaceService CreateService(IWorkspaceServiceProvider workspaceServices)
        {
            return new Factory();
        }

        private sealed class Factory : ILoggerFactoryService
        {
            public ILogger GetLogger()
            {
                return EmptyLogger.Instance;
            }
        }
    }
}
