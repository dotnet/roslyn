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
    /// logger factory service that will return logger
    /// </summary>
    internal interface ILoggerFactoryService : IWorkspaceService
    {
        /// <summary>
        /// return roslyn logger
        /// </summary>
        ILogger GetLogger();
    }
}
