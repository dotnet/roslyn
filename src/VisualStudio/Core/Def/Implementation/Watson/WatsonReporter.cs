// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.LanguageServices.Implementation.Watson;

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
            using (var report = WatsonErrorReport.CreateNonFatalReport(new ExceptionInfo(exception, "Roslyn")))
            {
                // Ignore the return value on purpose, we don't care if it actually gets submitted
                report.ReportIfNecessary();
            }
        }
    }
}
