// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal sealed class DiagnosticOptionsStorage
    {
        public static readonly Option2<bool> LspPullDiagnosticsFeatureFlag = new(
            "csharp_diagnostic_options_lsp_pull_diagnostics_feature_flag", defaultValue: false);

        public static readonly Option2<bool> LogTelemetryForBackgroundAnalyzerExecution = new(
            "dotnet_diagnostic_options_log_telemetry_for_background_analyzer_execution", defaultValue: false);
    }
}
