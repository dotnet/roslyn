// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ErrorLogger;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.Watson;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Log
{
    [ExportWorkspaceService(typeof(IErrorLoggerService), ServiceLayer.Host), Export(typeof(IErrorLoggerService)), Shared]
    internal class VisualStudioErrorLogger : IErrorLoggerService
    {
        public void LogException(object source, Exception exception)
        {
            var name = source.GetType().Name;
            ActivityLog.LogError(name, ToLogFormat(exception));

            if (ShouldReportCrashDumps(source))
            {
                using (var report = WatsonErrorReport.CreateNonFatalReport(new ExceptionInfo(exception, name)))
                {
                    report.ReportIfNecessary();
                }
            }
        }

        public bool TryLogException(object source, Exception exception)
        {
            bool watsonReportResult = true;
            var name = source.GetType().Name;


            if (ShouldReportCrashDumps(source))
            {
                using (var report = WatsonErrorReport.CreateNonFatalReport(new ExceptionInfo(exception, name)))
                {
                    watsonReportResult = report.ReportIfNecessary();
                }
            }

            var activityLogResult = ActivityLog.TryLogError(name, ToLogFormat(exception));
            return watsonReportResult && activityLogResult;
        }

        private bool ShouldReportCrashDumps(object source)
        {
            var refactoringAttributes = source.GetType()
                .GetCustomAttributes(typeof(ExportCodeRefactoringProviderAttribute), true)
                .Cast<ExportCodeRefactoringProviderAttribute>();
            if (PredefinedCodeRefactoringProviderNames.Any(name => refactoringAttributes.Select(x => x.Name).Any(n => n == name)))
            {
                return true;
            }
            var codeFixAttributes = source.GetType()
                .GetCustomAttributes(typeof(ExportCodeFixProviderAttribute), true)
                .Cast<ExportCodeFixProviderAttribute>();
            if (PredefinedCodeFixProviderNames.Any(name => refactoringAttributes.Select(x => x.Name).Any(n => n == name)))
            {
                return true;
            }

            return false;
        }

        private static IEnumerable<string> PredefinedCodeRefactoringProviderNames => s_lazyPredefinedCodeRefactoringProviderNames.Value;

        private static Lazy<IEnumerable<string>> s_lazyPredefinedCodeRefactoringProviderNames =
            new Lazy<IEnumerable<string>>(() => typeof(PredefinedCodeRefactoringProviderNames).GetFields().Select(f => f.GetRawConstantValue().ToString()));


        private static IEnumerable<string> PredefinedCodeFixProviderNames => s_lazyPredefinedCodeFixProviderNames.Value;

        private static Lazy<IEnumerable<string>> s_lazyPredefinedCodeFixProviderNames =
            new Lazy<IEnumerable<string>>(() => typeof(PredefinedCodeFixProviderNames).GetFields().Select(f => f.GetRawConstantValue().ToString()));

        private static string ToLogFormat(Exception exception)
        {
            return exception.Message + Environment.NewLine + exception.StackTrace;
        }
    }
}
