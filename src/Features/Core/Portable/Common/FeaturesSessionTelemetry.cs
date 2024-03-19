// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.Completion.Log;

namespace Microsoft.CodeAnalysis.Common;

internal static class FeaturesSessionTelemetry
{
    public static void Report()
    {
        CompletionProvidersLogger.ReportTelemetry();
        ChangeSignatureLogger.ReportTelemetry();
    }
}
