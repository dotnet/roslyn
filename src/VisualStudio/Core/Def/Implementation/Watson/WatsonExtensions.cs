// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Telemetry;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.ErrorReporting
{
    internal static class WatsonExtensions
    {
        // NFW API let caller to customize watson report to make them better bucketed by
        // putting custom string in reserved slots. normally those 3 slots will be empty.
        private const int Reserved1 = 7;
        private const int Reserved2 = 6;

        // replace exception slot for callstack since the exception (with empty callstack) 
        // given is synthesized one that doesn't provide any meaningful data
        private const int Reserved3 = 3;

        /// <summary>
        /// This sets extra watson bucket parameters to make bucketting better
        /// in non fatal watson report
        /// </summary>
        public static void SetExtraParameters(this IFaultUtility fault, Exception exception, bool emptyCallstack)
        {
            if (emptyCallstack)
            {
                // if exception we got started with empty callstack, put hash of runtime
                // callstack in one of reserved slot for better bucketting.
                // we put hash since NFW just takes certain length of callstack which
                // makes the callstack useless
                fault.SetBucketParameter(Reserved3, $"{Environment.StackTrace?.GetHashCode() ?? 0}");
            }

            switch (exception)
            {
                case RemoteInvocationException remote:
                    fault.SetBucketParameter(Reserved1, remote.GetParameterString());
                    return;
                case AggregateException aggregate:
                    if (aggregate.InnerException == null)
                    {
                        return;
                    }
                    else if (aggregate.InnerExceptions.Count == 1)
                    {
                        fault.SetBucketParameter(Reserved1, aggregate.GetParameterString());
                        return;
                    }
                    else
                    {
                        var flatten = aggregate.Flatten();

                        fault.SetBucketParameter(Reserved1, flatten.InnerException.GetParameterString());
                        fault.SetBucketParameter(Reserved2, flatten.CalculateHash());

                        return;
                    }
                default:
                    if (exception.InnerException == null)
                    {
                        return;
                    }

                    fault.SetBucketParameter(Reserved1, exception.InnerException.GetParameterString());
                    return;
            }
        }

        public static string CalculateHash(this AggregateException exception)
        {
            var hash = 1;
            foreach (var inner in exception.InnerExceptions)
            {
                var parameterString = inner.GetParameterString();
                hash = Hash.Combine(parameterString, hash);
            }

            return hash.ToString();
        }

        public static string GetParameterString(this Exception exception)
            => exception switch
            {
                RemoteInvocationException remote => $"{remote.ErrorCode} {remote.StackTrace ?? exception.Message}",
                AggregateException aggregate when aggregate.InnerException != null =>
                    // get first exception that is not aggregated exception
                    GetParameterString(aggregate.InnerException),
                _ => $"{exception.GetType().ToString()} {(exception.StackTrace ?? exception.ToString())}",
            };

        /// <summary>
        /// hold onto last issue we reported. we use hash
        /// since exception callstack could be quite big
        /// </summary>
        private static int s_lastExceptionReported;

#if DEBUG
        /// <summary>
        /// in debug, we also hold onto reported string to make debugging easier
        /// </summary>
        private static string s_lastExceptionReportedDebug;
#endif

        public static bool ShouldReport(this Exception exception)
        {
            // this is a poor man's check whether we are called for same issues repeatedly
            // one of problem of NFW compared to FW is that since we don't crash at an issue, same issue
            // might happen repeatedly. especially in short amount of time. reporting all those issues
            // are meaningless so we do cheap check to see we just reported same issue and
            // bail out.
            // I think this should be actually done by PostFault itself and I talked to them about it.
            // but until they do something, we will do very simple throuttle ourselves.
            var currentExceptionString = exception.GetParameterString();
            var currentException = currentExceptionString.GetHashCode();
            if (s_lastExceptionReported == currentException)
            {
                return false;
            }

#if DEBUG
            s_lastExceptionReportedDebug = currentExceptionString;
#endif

            s_lastExceptionReported = currentException;

            return true;
        }

        public static bool SetCallstackIfEmpty(this Exception exception)
        {
            // There have been cases where a new, unthrown exception has been passed to this method.
            // In these cases the exception won't have a stack trace, which isn't very helpful. We
            // throw and catch the exception here as that will result in a stack trace that is
            // better than nothing.
            if (exception.StackTrace != null)
            {
                return false;
            }

            try
            {
                throw exception;
            }
            catch
            {
                // Empty; we just need the exception to have a stack trace.
            }

            return true;
        }
    }
}
