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
            var aggregateException = exception as AggregateException;
            if (aggregateException != null)
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
            var fileNotFoundException = exception as FileNotFoundException;
            if (fileNotFoundException == null)
            {
                return exception.Message;
            }

            var fusionLog = DesktopShim.FileNotFoundException.TryGetFusionLog(fileNotFoundException);
            if (fusionLog == null)
            {
                return exception.Message;
            }

            return string.Join(s_separator, fileNotFoundException.Message, fusionLog);
        }
    }
}
