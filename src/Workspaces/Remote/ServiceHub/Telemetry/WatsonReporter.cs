// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.VisualStudio.LanguageServices.Telemetry;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.CodeAnalysis.ErrorReporting
{
    internal class WatsonReporter
    {
        /// <summary>
        /// The default callback to pass to <see cref="TelemetrySessionExtensions.PostFault(TelemetrySession, string, string, Exception, Func{IFaultUtility, int})"/>.
        /// Returning "0" signals that we should send data to Watson; any other value will cancel the Watson report.
        /// </summary>
        private static Func<IFaultUtility, int> s_defaultCallback = _ => 0;

        /// <summary>
        /// Report Non-Fatal Watson
        /// </summary>
        /// <param name="exception">Exception that triggered this non-fatal error</param>
        /// <param name="severity">indicate <see cref="WatsonSeverity"/> of NFW</param>
        public static void Report(Exception exception, WatsonSeverity severity = WatsonSeverity.Default)
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

            // if given exception is non recoverable exception,
            // crash instead of NFW
            if (IsNonRecoverableException(exception))
            {
                CodeAnalysis.FailFast.OnFatalException(exception);
            }

            if (!exception.ShouldReport())
            {
                return;
            }

            if (RoslynServices.SessionOpt == null)
            {
                return;
            }

            // in OOP, we don't fire Critical NFW, rather we fire General which is 1 level higher than Diagnostic
            // and we keep fire NFW even after critical report. 
            // critical NFW regarding OOP from VS side will let us to prioritize NFW to fix, and NFW from here should provide
            // extra dump/info to take a look.
            // whenever there is an exception in OOP, we fire NFW in both VS and OOP side. VS will report it as critical NFW
            // and OOP will fire normal NFW. we only mark VS side critical since we don't want to double report same issue
            // and don't want to shutdown NFW in OOP
            // one can correlate NFW from VS and OOP through remote callstack in VS NFW
            var faultEvent = new FaultEvent(
                eventName: FunctionId.NonFatalWatson.GetEventName(),
                description: description,
                critical ? FaultSeverity.General : FaultSeverity.Diagnostic,
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

            RoslynServices.SessionOpt.PostEvent(faultEvent);
        }

        private static bool IsNonRecoverableException(Exception exception)
        {
            return exception is OutOfMemoryException;
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
