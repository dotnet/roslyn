using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ErrorLogger
{
    interface IErrorLoggerService : IWorkspaceService
    {
        void LogException(object source, Exception exception);
        bool TryLogException(object source, Exception exception);
    }
}
