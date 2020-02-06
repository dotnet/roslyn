﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.ErrorLogger;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Shell;
using static Microsoft.CodeAnalysis.RoslynAssemblyHelper;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Log
{
    [ExportWorkspaceService(typeof(IErrorLoggerService), ServiceLayer.Host), Export(typeof(IErrorLoggerService)), Shared]
    internal class VisualStudioErrorLogger : IErrorLoggerService
    {
        [ImportingConstructor]
        public VisualStudioErrorLogger()
        {
        }

        public void LogException(object source, Exception exception)
        {
            var name = source.GetType().Name;
            ActivityLog.LogError(name, ToLogFormat(exception));

            if (ShouldReportCrashDumps(source))
            {
                WatsonReporter.Report(name, exception);
            }
        }

        private bool ShouldReportCrashDumps(object source) => HasRoslynPublicKey(source);

        private static string ToLogFormat(Exception exception)
        {
            return exception.Message + Environment.NewLine + exception.StackTrace;
        }
    }
}
