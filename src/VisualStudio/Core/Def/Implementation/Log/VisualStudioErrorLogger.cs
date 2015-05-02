using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
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

            if (source is ReportCrashDumpsToMicrosoft)
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

            if (source is ReportCrashDumpsToMicrosoft)
            {
                using (var report = WatsonErrorReport.CreateNonFatalReport(new ExceptionInfo(exception, name)))
                {
                    watsonReportResult = report.ReportIfNecessary();
                }
            }

            var activityLogResult = ActivityLog.TryLogError(name, ToLogFormat(exception));
            return watsonReportResult && activityLogResult;
        }

        private static string ToLogFormat(Exception exception)
        {
            return exception.Message + Environment.NewLine + exception.StackTrace;
        }
    }
}
