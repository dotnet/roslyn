// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Linq;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class AnalyzerExceptionDescriptionBuilder
    {
        // Description separator
        private static readonly string s_separator = Environment.NewLine + "-----" + Environment.NewLine;

        public static string CreateDiagnosticDescription(this Exception exception)
        {
            if (exception is AggregateException aggregateException)
            {
                var flattened = aggregateException.Flatten();
                return string.Join(s_separator, flattened.InnerExceptions.Select(e => GetExceptionMessage(e)));
            }

            if (exception != null)
            {
                return string.Join(s_separator, GetExceptionMessage(exception), CreateDiagnosticDescription(exception.InnerException));
            }

            return string.Empty;
        }

        private static string GetExceptionMessage(Exception exception)
        {
            var fusionLog = (exception as FileNotFoundException)?.FusionLog;
            if (fusionLog == null)
            {
                return exception.ToString();
            }

            return string.Join(s_separator, exception.Message, fusionLog);
        }
    }
}
