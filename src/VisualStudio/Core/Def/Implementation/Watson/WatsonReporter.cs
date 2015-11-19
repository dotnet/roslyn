// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Text;
using System.Reflection;
using Microsoft.VisualStudio.LanguageServices.Implementation.Watson;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal class WatsonReporter
    {
        /// <summary>
        /// Report Non-Fatal Watson
        /// </summary>
        /// <param name="exception">Exception that triggered this non-fatal error</param>
        public static void Report(Exception exception)
        {
            // Log the exception regardless of whether the Watson will be throttled.
            Logger.Log(FunctionId.NonFatalWatson, KeyValueLogMessage.Create(dict =>
            {
                dict["ExceptionType"] = exception.GetType().FullName;
                dict["ExceptionStack"] = GetStackTraceString(exception);
            }));

            using (var report = WatsonErrorReport.CreateNonFatalReport(new ExceptionInfo(exception, "Roslyn")))
            {
                // Ignore the return value on purpose, we don't care if it actually gets submitted
                report.ReportIfNecessary();
            }
        }

        /// <remarks>
        /// <see cref="Exception.StackTrace"/> produces inconvenient strings: 
        /// there are line breaks and "at" is localized.
        /// </remarks>
        private static string GetStackTraceString(Exception e)
        {
            PooledStringBuilder pooled = PooledStringBuilder.GetInstance();
            StringBuilder builder = pooled.Builder;

            // No file info, since that would disrupt bucketing.
            foreach (StackFrame frame in new StackTrace(e, fNeedFileInfo: false).GetFrames())
            {
                MethodBase method = frame.GetMethod();
                if (method == null) continue;

                // method.ToString() does not include the declaring type, so we'll prepend it manually.
                // It will appear before the return type, but that won't disrupt our telemetry.
                builder.Append(method.ReflectedType.ToString().Replace(',', '\''));
                builder.Append("::");
                builder.Append(method.ToString().Replace(',', '\''));
                builder.Append(';'); // Method signatures should not contain semicolons.
            }

            return pooled.ToStringAndFree();
        }
    }
}
