// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Roslyn.Utilities;

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
            if (!(exception is FileNotFoundException fileNotFoundException))
            {
                return exception.ToString();
            }

            var fusionLog = DesktopShim.FileNotFoundExceptionShim.TryGetFusionLog(fileNotFoundException);
            if (fusionLog == null)
            {
                return exception.ToString();
            }

            return string.Join(s_separator, fileNotFoundException.Message, fusionLog);
        }
    }
}
