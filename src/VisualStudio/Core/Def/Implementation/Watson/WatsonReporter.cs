// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.LanguageServices.Telemetry;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.ErrorReporting
{
    /// <summary>
    /// Controls whether or not we actually report the failure.
    /// There are situations where we know we're in a bad state and any further reports are unlikely to be
    /// helpful, so we shouldn't send them.
    /// </summary>
    internal static class WatsonDisabled
    {
        // we have it this way to make debugging easier since VS debugger can't reach
        // static type with same fully qualified name in multiple dlls.
        public static bool s_reportWatson = true;
    }

    internal static class WatsonReporter
    {
        /// <summary>
        /// Collection of all asynchronous reporting tasks that are started.
        /// </summary>
        private static JoinableTaskCollection s_asyncTasks = new JoinableTaskCollection(ThreadHelper.JoinableTaskContext);

        /// <summary>
        /// Wait for started reporting tasks to finish. 
        /// This should be called during shutdown to avoid dropping NFW.
        /// </summary>
        public static void WaitForPendingReporting()
#pragma warning disable VSTHRD102 // Implement internal logic asynchronously
            => ThreadHelper.JoinableTaskFactory.Run(async () => await s_asyncTasks.JoinTillEmptyAsync().ConfigureAwait(false));
#pragma warning restore VSTHRD102 // Implement internal logic asynchronously

        /// <summary>
        /// The default callback to pass to <see cref="TelemetrySessionExtensions.PostFault(TelemetrySession, string, string, Exception, Func{IFaultUtility, int})"/>.
        /// Returning "0" signals that we should send data to Watson; any other value will cancel the Watson report.
        /// </summary>
        private static Func<IFaultUtility, int> s_defaultCallback = _ => 0;

        /// <summary>
        /// Report Non-Fatal Watson
        /// </summary>
        /// <param name="exception">Exception that triggered this non-fatal error</param>
        public static void Report(Exception exception)
        {
            Report("Roslyn NonFatal Watson", exception, WatsonSeverity.Default);
        }

        /// <summary>
        /// Report Non-Fatal Watson
        /// </summary>
        /// <param name="exception">Exception that triggered this non-fatal error</param>
        /// <param name="severity">indicate <see cref="WatsonSeverity"/> of NFW</param>
        public static void Report(Exception exception, WatsonSeverity severity)
        {
            Report("Roslyn NonFatal Watson", exception, severity);
        }

        /// <summary>
        /// Report Non-Fatal Watson
        /// </summary>
        /// <param name="description">any description you want to save with this watson report</param>
        /// <param name="exception">Exception that triggered this non-fatal error</param>
        /// <param name="severity">indicate <see cref="WatsonSeverity"/> of NFW</param>
        public static void Report(string description, Exception exception, WatsonSeverity severity = WatsonSeverity.Default)
        {
            Report(description, exception, s_defaultCallback, severity);
        }

        /// <summary>
        /// Report Non-Fatal Watson
        /// </summary>
        /// <param name="description">any description you want to save with this watson report</param>
        /// <param name="exception">Exception that triggered this non-fatal error</param>
        /// <param name="callback">Callback to include extra data with the NFW. Note that we always collect
        /// a dump of the current process, but this can be used to add further information or files to the
        /// CAB.</param>
        /// <param name="severity">indicate <see cref="WatsonSeverity"/> of NFW</param>
        public static void Report(string description, Exception exception, Func<IFaultUtility, int> callback, WatsonSeverity severity = WatsonSeverity.Default)
        {
            var critical = severity == WatsonSeverity.Critical;
            var emptyCallstack = exception.SetCallstackIfEmpty();

            if (!WatsonDisabled.s_reportWatson ||
                !exception.ShouldReport())
            {
                return;
            }

            var faultEvent = new FaultEvent(
                eventName: FunctionId.NonFatalWatson.GetEventName(),
                description: description,
                critical ? FaultSeverity.Critical : FaultSeverity.Diagnostic,
                exceptionObject: exception,
                gatherEventDetails: arg =>
                {
                    // always add current processes dump
                    arg.AddProcessDump(System.Diagnostics.Process.GetCurrentProcess().Id);

                    return callback(arg);
                });

            // add extra bucket parameters to bucket better in NFW
            // we do it here so that it gets bucketted better in both
            // watson and telemetry. 
            faultEvent.SetExtraParameters(exception, emptyCallstack);

            s_asyncTasks.Add(ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                // Make sure we do not block UI thread on filing Watson reports.
                await TaskScheduler.Default;
                TelemetryService.DefaultSession.PostEvent(faultEvent);
            }));

            if (exception is OutOfMemoryException || critical)
            {
                // Once we've encountered one OOM or Critial NFW, 
                // we're likely to see more. There will probably be other
                // failures as a direct result of the OOM or critical NFW, as well. 
                // These aren't helpful so we should just stop reporting failures.
                WatsonDisabled.s_reportWatson = false;
            }
        }
    }

    internal enum WatsonSeverity
    {
        /// <summary>
        /// Indicate that this watson is informative and not urgent
        /// </summary>
        Default,

        /// <summary>
        /// Indicate that this watson is critical and need to be addressed soon
        /// </summary>
        Critical,
    }
}
