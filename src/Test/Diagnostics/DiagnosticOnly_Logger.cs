// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;

namespace Roslyn.Hosting.Diagnostics
{
    /// <summary>
    /// provide a way to access internal logging framework
    /// </summary>
    public static class DiagnosticOnly_Logger
    {
        /// <summary>
        /// get roslyn event source name
        /// </summary>
        public static string GetRoslynEventSourceName()
        {
            return EventSource.GetName(typeof(RoslynEventSource));
        }

        /// <summary>
        /// get roslyn event source guid
        /// </summary>
        public static Guid GetRoslynEventSourceGuid()
        {
            return EventSource.GetGuid(typeof(RoslynEventSource));
        }

        /// <summary>
        /// reset logger to default one
        /// </summary>
        public static void ResetLogger()
        {
            Logger.SetLogger(null);
        }

        /// <summary>
        /// let one such as ETA to set logger for the service layer
        /// </summary>
        internal static void SetLogger(IOptionService optionsService, string loggerName)
        {
            if (loggerName == null)
            {
                ResetLogger();
            }

            Logger.SetLogger(GetLogger(optionsService, loggerName));
        }

        /// <summary>
        /// get string representation of functionId
        /// </summary>
        public static string GetFunctionId(int functionId)
        {
            return ((FunctionId)functionId).ToString();
        }

        /// <summary>
        /// use Roslyn Logger from outside
        /// </summary>
        public static IDisposable LogBlock(string functionId)
        {
            return Logger.LogBlock(GetFunctionId(functionId), CancellationToken.None);
        }

        /// <summary>
        /// use Roslyn Logger from outside
        /// </summary>
        public static IDisposable LogBlock(string functionId, string message)
        {
            return Logger.LogBlock(GetFunctionId(functionId), message, CancellationToken.None);
        }

        /// <summary>
        /// get given functionId's int value
        /// </summary>
        public static int GetFunctionIdValue(string functionId)
        {
            // this will throw if given functionid doesn't exist
            return (int)GetFunctionId(functionId);
        }

        private static FunctionId GetFunctionId(string functionId)
        {
            return (FunctionId)Enum.Parse(typeof(FunctionId), functionId);
        }

        private static ILogger GetLogger(IOptionService optionsService, string loggerName)
        {
            switch (loggerName)
            {
                case "EtwLogger":
                    return new EtwLogger(Logger.GetLoggingChecker(optionsService));
                case "TraceLogger":
                    return new TraceLogger(Logger.GetLoggingChecker(optionsService));
                default:
                    return EmptyLogger.Instance;
            }
        }
    }
}
