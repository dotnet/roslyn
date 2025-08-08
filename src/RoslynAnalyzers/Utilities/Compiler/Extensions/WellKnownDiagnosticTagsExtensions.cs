// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    internal static class WellKnownDiagnosticTagsExtensions
    {
        public const string EnabledRuleInAggressiveMode = nameof(EnabledRuleInAggressiveMode);
        public const string Dataflow = nameof(Dataflow);
        public const string CompilationEnd = nameof(CompilationEnd);
        public static readonly string[] DataflowAndTelemetry = [Dataflow, WellKnownDiagnosticTags.Telemetry];
        public static readonly string[] DataflowAndTelemetryEnabledInAggressiveMode = [Dataflow, WellKnownDiagnosticTags.Telemetry, EnabledRuleInAggressiveMode];

        public static readonly string[] Telemetry = [WellKnownDiagnosticTags.Telemetry];
        public static readonly string[] TelemetryEnabledInAggressiveMode = [WellKnownDiagnosticTags.Telemetry, EnabledRuleInAggressiveMode];
        public static readonly string[] CompilationEndAndTelemetry = [CompilationEnd, WellKnownDiagnosticTags.Telemetry];
    }
}
