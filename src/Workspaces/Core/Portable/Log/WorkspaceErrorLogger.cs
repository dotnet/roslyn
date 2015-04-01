using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorLogger;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.ErrorLogger
{
    [ExportWorkspaceService(typeof(IErrorLoggerService)), Export(typeof(IErrorLoggerService)), Shared]
    class WorkspaceErrorLogger : IErrorLoggerService
    {
        public void LogError(string source, string message)
        {
            Logger.GetLogger()?.Log(FunctionId.Extension_Exception, LogMessage.Create(source + " : " + message));
        }

        public bool TryLogError(string source, string message)
        {
            var logger = Logger.GetLogger();
            if (logger != null)
            {
                logger.Log(FunctionId.Extension_Exception, LogMessage.Create(source + " : " + message));
                return true;
            }

            return false;
        }
    }
}

