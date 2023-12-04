// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.Completion.Log;
using Microsoft.CodeAnalysis.Logging;

namespace Microsoft.CodeAnalysis.Common
{
    internal static class FeaturesSessionTelemetry
    {
        public static void Report()
        {
            CompletionProvidersLogger.ReportTelemetry();
            SolutionLogger.ReportTelemetry();
            ChangeSignatureLogger.ReportTelemetry();
        }
    }
}
