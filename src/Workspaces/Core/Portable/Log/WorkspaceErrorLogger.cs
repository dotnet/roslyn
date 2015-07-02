﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    internal class WorkspaceErrorLogger : IErrorLoggerService
    {
        public void LogException(object source, Exception exception)
        {
            Logger.GetLogger()?.Log(FunctionId.Extension_Exception, LogMessage.Create(source.GetType().Name + " : " + ToLogFormat(exception)));
        }

        public bool TryLogException(object source, Exception exception)
        {
            var logger = Logger.GetLogger();
            var name = source.GetType().Name;

            if (logger != null)
            {
                logger.Log(FunctionId.Extension_Exception, LogMessage.Create(name + " : " + ToLogFormat(exception)));
                return true;
            }

            return false;
        }

        private static string ToLogFormat(Exception exception)
        {
            return exception.Message + Environment.NewLine + exception.StackTrace;
        }
    }
}

