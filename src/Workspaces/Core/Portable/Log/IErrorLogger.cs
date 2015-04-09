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
        void LogError(string source, string message);
        bool TryLogError(string source, string message);
    }
}
